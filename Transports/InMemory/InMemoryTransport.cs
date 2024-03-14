using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Exanite.Core.Collections;
using Exanite.Core.Events;

namespace Exanite.Networking.Transports.InMemory
{
    public class InMemoryTransport : ITransport
    {
        public static TwoWayDictionary<int, InMemoryTransport> Servers { get; } = new();

        private TwoWayDictionary<int, InMemoryTransport> connections = new();

        private Queue<TransportConnectionStatusEventArgs> connectionEventQueue = new();
        private Queue<TransportDataReceivedEventArgs> dataEventQueue = new();

        private int nextConnectionId = 0;

        public InMemoryTransportSettings Settings { get; }

        public INetwork Network { get; set; }
        public LocalConnectionStatus Status { get; protected set; }

        public event EventHandler<ITransport, TransportDataReceivedEventArgs> DataReceived;
        public event EventHandler<ITransport, TransportConnectionStatusEventArgs> ConnectionStatus;

        public InMemoryTransport(InMemoryTransportSettings settings)
        {
            Settings = settings;
        }

        public void Dispose()
        {
            StopConnection(false);
        }

        public void Tick()
        {
            PushEvents();
        }

        public async UniTask StartConnection()
        {
            if (Network.IsServer)
            {
                if (!Servers.TryAdd(Settings.VirtualPort, this))
                {
                    throw new NetworkException($"Virtual port {Settings.VirtualPort} is already in use.");
                }

                Status = LocalConnectionStatus.Started;
            }

            if (Network.IsClient)
            {
                // Prevent one frame delay issues when both server and client are started at the same time.
                await UniTask.Yield();

                if (!Servers.TryGetValue(Settings.VirtualPort, out var server))
                {
                    throw new NetworkException($"No {typeof(InMemoryTransport).Name} server active on virtual port {Settings.VirtualPort}.");
                }

                server.OnClientConnected(this);

                Status = LocalConnectionStatus.Started;
            }
        }

        public void StopConnection()
        {
            StopConnection(true);

            Servers.Inverse.Remove(this);
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

        private void OnClientConnected(InMemoryTransport client)
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
