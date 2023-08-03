using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Exanite.Core.Collections;
using Exanite.Core.Events;
using UniDi;
using UnityEngine;

namespace Exanite.Networking.Transports.InMemory
{
    public abstract class InMemoryTransport : MonoBehaviour, ITransport
    {
        private TwoWayDictionary<int, InMemoryTransport> connections;

        private Queue<TransportConnectionStatusEventArgs> connectionEventQueue;
        private Queue<TransportDataReceivedEventArgs> dataEventQueue;

        private int nextConnectionId = 0;

        [Inject] protected InMemoryTransportSettings Settings { get; }

        public LocalConnectionStatus Status { get; protected set; }

        public event EventHandler<ITransport, TransportDataReceivedEventArgs> DataReceived;
        public event EventHandler<ITransport, TransportConnectionStatusEventArgs> ConnectionStatus;

        private void Awake()
        {
            connections = new();
            connectionEventQueue = new();
            dataEventQueue = new();
        }

        private void OnDestroy()
        {
            StopConnection(false);
        }

        public void Tick()
        {
            PushEvents();
        }

        public abstract UniTask StartConnection();

        public void StopConnection()
        {
            StopConnection(true);
        }

        protected virtual void StopConnection(bool handleEvents)
        {
            try
            {
                if (handleEvents)
                {
                    PushEvents();
                }
            }
            finally
            {
                connections.Clear();

                Status = LocalConnectionStatus.Stopped;
            }
        }

        public RemoteConnectionStatus GetConnectionStatus(int connectionId)
        {
            return connections.ContainsKey(connectionId) ? RemoteConnectionStatus.Started : RemoteConnectionStatus.Stopped;
        }

        public void DisconnectConnection(int connectionId)
        {
            if (connections.TryGetValue(connectionId, out var remoteTransport))
            {
                connections.Remove(connectionId);
                connectionEventQueue.Enqueue(new TransportConnectionStatusEventArgs(connectionId, RemoteConnectionStatus.Stopped));

                if (remoteTransport.connections.Inverse.TryGetValue(this, out var remoteId))
                {
                    remoteTransport.connections.Remove(remoteId);
                    remoteTransport.connectionEventQueue.Enqueue(new TransportConnectionStatusEventArgs(remoteId, RemoteConnectionStatus.Stopped));
                }
            }
        }

        public void SendData(int connectionId, ArraySegment<byte> data, SendType sendType)
        {
            if (!connections.TryGetValue(connectionId, out var remoteTransport))
            {
                throw new NetworkException("Attempted to send data to invalid connection.");
            }

            if (!remoteTransport.connections.Inverse.TryGetValue(this, out var remoteId))
            {
                throw new NetworkException("Missing connection on remote. Local and remote connection lists are mismatched.");
            }

            // Currently always sends reliably.
            // Todo Pool arrays.
            remoteTransport.dataEventQueue.Enqueue(new TransportDataReceivedEventArgs(remoteId, data.ToArray(), sendType));
        }

        protected void PushEvents()
        {
            while (connectionEventQueue.TryDequeue(out var e))
            {
                ConnectionStatus?.Invoke(this, e);
            }

            while (dataEventQueue.TryDequeue(out var e))
            {
                DataReceived?.Invoke(this, e);
            }
        }

        /// <summary>
        /// Only called from <see cref="InMemoryTransportClient"/>.
        /// </summary>
        /// <remarks>
        /// It's bad practice to refer to inheriting classes in the base class, but it's the easiest way to keep the API mostly hidden.
        /// </remarks>
        public void OnClientConnected(InMemoryTransportClient client)
        {
            if (connections.Inverse.ContainsKey(client) || client.connections.Inverse.ContainsKey(this))
            {
                throw new NetworkException("Already connected.");
            }

            connections.Add(nextConnectionId, client);
            connectionEventQueue.Enqueue(new TransportConnectionStatusEventArgs(nextConnectionId, RemoteConnectionStatus.Started));
            nextConnectionId++;

            client.connections.Add(client.nextConnectionId, this);
            client.connectionEventQueue.Enqueue(new TransportConnectionStatusEventArgs(client.nextConnectionId, RemoteConnectionStatus.Started));
            client.nextConnectionId++;
        }
    }
}
