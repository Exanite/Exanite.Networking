using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Exanite.Core.Utilities;
using Exanite.Networking.Transports;
using LiteNetLib.Utils;
using Sirenix.OdinInspector;

namespace Exanite.Networking
{
    public abstract class Network : SerializedMonoBehaviour, INetwork
    {
        private ConnectionTracker connectionTracker;
        private Dictionary<int, IPacketHandler> packetHandlers;

        private NetDataReader cachedReader;
        private NetDataWriter cachedWriter;

        private Queue<ConnectionStatusEventArgs> eventQueue;
        private LocalConnectionStatus previousStatus;

        public LocalConnectionStatus Status { get; protected set; }
        public virtual bool IsReady => Status == LocalConnectionStatus.Started;

        public IReadOnlyDictionary<int, IPacketHandler> PacketHandlers => packetHandlers;
        public IReadOnlyDictionary<int, NetworkConnection> Connections => connectionTracker.Connections;

        public event ConnectionStatusEvent ConnectionStatus;
        public event ConnectionStartedEvent ConnectionStarted;
        public event ConnectionStoppedEvent ConnectionStopped;

        protected virtual void Awake()
        {
            var connectionFactory = new ConnectionFactory();
            connectionTracker = new ConnectionTracker(connectionFactory);
            packetHandlers = new Dictionary<int, IPacketHandler>();

            cachedReader = new NetDataReader();
            cachedWriter = new NetDataWriter();

            eventQueue = new Queue<ConnectionStatusEventArgs>();

            connectionTracker.ConnectionAdded += OnConnectionStarted;
            connectionTracker.ConnectionRemoved += OnConnectionStopped;
        }

        protected virtual void OnDestroy()
        {
            connectionTracker.ConnectionAdded -= OnConnectionStarted;
            connectionTracker.ConnectionRemoved -= OnConnectionStopped;
        }

        protected virtual void FixedUpdate()
        {
            Tick();
        }

        public void Tick()
        {
            if (Status == LocalConnectionStatus.Started && !AreTransportsAllStarted())
            {
                StopConnection();
            }

            OnTickTransports();
            PushEvents();
        }

        public abstract UniTask StartConnection();

        public abstract void StopConnection();

        public void RegisterPacketHandler(IPacketHandler handler)
        {
            packetHandlers.Add(handler.HandlerId, handler);
        }

        public void UnregisterPacketHandler(IPacketHandler handler)
        {
            packetHandlers.Remove(handler.HandlerId);
        }

        public void SendAsPacketHandler(IPacketHandler handler, NetworkConnection connection, NetDataWriter writer, SendType sendType)
        {
            ValidateIsReadyToSend();

            WritePacketHandlerDataToCachedWriter(handler, writer);

            var data = new ArraySegment<byte>(cachedWriter.Data, 0, cachedWriter.Length);
            connection.Transport.SendData(connection.TransportConnectionId, data, sendType);
        }

        protected void ValidateIsReadyToSend()
        {
            if (!IsReady)
            {
                throw new InvalidOperationException($"{GetType()} is not ready to send.");
            }
        }

        protected void ValidateIsStopped()
        {
            switch (Status)
            {
                case LocalConnectionStatus.Starting: throw new InvalidOperationException($"{GetType().Name} is already starting.");
                case LocalConnectionStatus.Started: throw new InvalidOperationException($"{GetType().Name} is already started.");
            }
        }

        protected abstract bool AreTransportsAllStarted();

        protected void RegisterTransportEvents(ITransport transport)
        {
            transport.ConnectionStatus += Transport_OnConnectionStatus;
            transport.ReceivedData += Transport_OnReceivedData;
        }

        protected void UnregisterTransportEvents(ITransport transport)
        {
            transport.ConnectionStatus += Transport_OnConnectionStatus;
            transport.ReceivedData -= Transport_OnReceivedData;
        }

        protected virtual void OnTickTransports() {}

        private void WritePacketHandlerDataToCachedWriter(IPacketHandler handler, NetDataWriter writer)
        {
            cachedWriter.Reset();

            cachedWriter.Put(handler.HandlerId);
            cachedWriter.Put(writer.Data, 0, writer.Length);
        }

        private void PushEvents()
        {
            void ProcessEventQueue()
            {
                while (eventQueue.TryDequeue(out var e))
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

                    ConnectionStatus?.Invoke(this, e.Connection, e.Status);
                }
            }

            if (previousStatus == LocalConnectionStatus.Stopped && Status == LocalConnectionStatus.Started)
            {
                previousStatus = Status;

                foreach (var packetHandler in packetHandlers.Values)
                {
                    packetHandler.OnNetworkStarted(this);
                }
            }

            if (Status == LocalConnectionStatus.Started)
            {
                ProcessEventQueue();
            }

            if (previousStatus == LocalConnectionStatus.Started && Status == LocalConnectionStatus.Stopped)
            {
                previousStatus = Status;

                ProcessEventQueue();

                foreach (var packetHandler in packetHandlers.Values)
                {
                    packetHandler.OnNetworkStopped(this);
                }
            }
        }

        private void OnConnectionStarted(NetworkConnection connection)
        {
            eventQueue.Enqueue(new ConnectionStatusEventArgs(connection, RemoteConnectionStatus.Started));
        }

        private void OnConnectionStopped(NetworkConnection connection)
        {
            eventQueue.Enqueue(new ConnectionStatusEventArgs(connection, RemoteConnectionStatus.Stopped));
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

        private void Transport_OnReceivedData(ITransport transport, TransportReceivedDataEventArgs e)
        {
            var connection = connectionTracker.GetNetworkConnection(transport, e.ConnectionId);
            if (connection == null)
            {
                return;
            }

            cachedReader.SetSource(e.Data.Array, e.Data.Offset, e.Data.Offset + e.Data.Count);

            var packetHandlerId = cachedReader.GetInt();
            if (!packetHandlers.TryGetValue(packetHandlerId, out var packetHandler))
            {
                return;
            }

            packetHandler.OnReceive(this, connection, cachedReader, e.SendType);
        }
    }
}
