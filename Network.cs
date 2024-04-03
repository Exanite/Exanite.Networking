using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Exanite.Core.Utilities;
using LiteNetLib.Utils;
using Exanite.Networking.Internal;
using Exanite.Networking.Transports;
using UnityEngine;

namespace Exanite.Networking
{
    public abstract class Network : ISimpleNetwork, IDisposable
    {
        private ConnectionTracker connectionTracker;

        private NetDataReader cachedReader = new();
        private NetDataWriter cachedWriter = new();

        private Queue<ConnectionStatusEventArgs> connectionEventQueue = new();
        private LocalConnectionStatus previousStatus;

        public abstract bool IsServer { get; }
        public bool IsClient => !IsServer;

        public LocalConnectionStatus Status { get; protected set; }
        public virtual bool IsReady => Status == LocalConnectionStatus.Started;

        public IEnumerable<NetworkConnection> Connections => connectionTracker.Connections.Values;

        public event NetworkStartedEvent NetworkStarted;
        public event NetworkStoppedEvent NetworkStopped;
        public event NetworkDataReceived NetworkDataReceived;

        public event ConnectionStartedEvent ConnectionStarted;
        public event ConnectionStoppedEvent ConnectionStopped;

        public Network()
        {
            var connectionFactory = new ConnectionFactory();
            connectionTracker = new ConnectionTracker(this, connectionFactory);

            connectionTracker.ConnectionAdded += OnConnectionStarted;
            connectionTracker.ConnectionRemoved += OnConnectionStopped;
        }

        public virtual void Dispose()
        {
            connectionTracker.ConnectionAdded -= OnConnectionStarted;
            connectionTracker.ConnectionRemoved -= OnConnectionStopped;
        }

        public void Tick()
        {
            if (Status == LocalConnectionStatus.Started && AreAnyTransportsStopped())
            {
                StopConnection();
            }

            OnTickTransports();
            PushEvents();
        }

        public abstract UniTask StartConnection();

        public abstract void StopConnection();

        public void Send(NetworkConnection connection, NetDataWriter writer, SendType sendType)
        {
            if (!IsReady)
            {
                throw new InvalidOperationException($"{GetType().Name} is not ready to send.");
            }

            if (!connection.Transport.IsReady)
            {
                throw new InvalidOperationException($"{GetType().Name} is not ready to send on transport {connection.Transport.GetType().Name}.");
            }

            var data = new ArraySegment<byte>(cachedWriter.Data, 0, cachedWriter.Length);
            connection.Transport.SendData(connection.TransportConnectionId, data, sendType);
        }

        protected void ValidateIsStopped()
        {
            switch (Status)
            {
                case LocalConnectionStatus.Starting: throw new InvalidOperationException($"{GetType().Name} is already starting.");
                case LocalConnectionStatus.Started: throw new InvalidOperationException($"{GetType().Name} is already started.");
            }
        }

        protected abstract bool AreAnyTransportsStopped();

        protected void RegisterTransportEvents(ITransport transport)
        {
            transport.ConnectionStatus += Transport_OnConnectionStatus;
            transport.DataReceived += Transport_OnDataReceived;
        }

        protected void UnregisterTransportEvents(ITransport transport)
        {
            transport.ConnectionStatus += Transport_OnConnectionStatus;
            transport.DataReceived -= Transport_OnDataReceived;
        }

        protected virtual void OnTickTransports() {}

        private void PushEvents()
        {
            void ProcessEventQueue()
            {
                while (connectionEventQueue.TryDequeue(out var e))
                {
                    switch (e.Status)
                    {
                        case RemoteConnectionStatus.Started:
                        {
                            ConnectionStarted?.Invoke(this, e.Connection);

                            break;
                        }
                        case RemoteConnectionStatus.Stopped:
                        {
                            ConnectionStopped?.Invoke(this, e.Connection);

                            break;
                        }
                        default: throw ExceptionUtility.NotSupportedEnumValue(e.Status);
                    }
                }
            }

            if (previousStatus == LocalConnectionStatus.Stopped && Status == LocalConnectionStatus.Started)
            {
                previousStatus = Status;

                NetworkStarted?.Invoke(this);
            }

            if (Status == LocalConnectionStatus.Started)
            {
                ProcessEventQueue();
            }

            if (previousStatus == LocalConnectionStatus.Started && Status == LocalConnectionStatus.Stopped)
            {
                previousStatus = Status;
                ProcessEventQueue();

                NetworkStopped?.Invoke(this);
            }
        }

        private void OnConnectionStarted(NetworkConnection connection)
        {
            connectionEventQueue.Enqueue(new ConnectionStatusEventArgs(connection, RemoteConnectionStatus.Started));
        }

        private void OnConnectionStopped(NetworkConnection connection)
        {
            connectionEventQueue.Enqueue(new ConnectionStatusEventArgs(connection, RemoteConnectionStatus.Stopped));
        }

        private void Transport_OnConnectionStatus(ITransport transport, TransportConnectionStatusEventArgs e)
        {
            switch (e.Status)
            {
                case RemoteConnectionStatus.Started:
                {
                    connectionTracker.AddNetworkConnection(transport, e.ConnectionId);

                    break;
                }
                case RemoteConnectionStatus.Stopped:
                {
                    connectionTracker.RemoveNetworkConnection(transport, e.ConnectionId);

                    break;
                }
                default: throw ExceptionUtility.NotSupportedEnumValue(e.Status);
            }
        }

        private void Transport_OnDataReceived(ITransport transport, TransportDataReceivedEventArgs e)
        {
            // This also prevents data from disconnected connections from being processed (intentional)
            // This is because connectionTracker always has the current list of active connections
            var connection = connectionTracker.GetNetworkConnection(transport, e.ConnectionId);
            if (connection == null)
            {
                Debug.LogWarning($"Received data from invalid connection {e.ConnectionId}.");

                return;
            }

            cachedReader.SetSource(e.Data.Array, e.Data.Offset, e.Data.Offset + e.Data.Count);
            NetworkDataReceived?.Invoke(this, connection, cachedReader, e.SendType);
        }
    }
}
