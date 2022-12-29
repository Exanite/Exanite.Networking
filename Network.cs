using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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

        private bool hasNotifiedPacketHandlersOfStart;

        public LocalConnectionStatus Status { get; protected set; }
        public virtual bool IsReady => Status == LocalConnectionStatus.Started;

        public IReadOnlyDictionary<int, IPacketHandler> PacketHandlers => packetHandlers;
        public IReadOnlyDictionary<int, NetworkConnection> Connections => connectionTracker.Connections;

        public event ConnectionStartedEvent ConnectionStarted;
        public event ConnectionStoppedEvent ConnectionStopped;

        protected virtual void Awake()
        {
            var connectionFactory = new ConnectionFactory();
            connectionTracker = new ConnectionTracker(connectionFactory);
            packetHandlers = new Dictionary<int, IPacketHandler>();

            cachedReader = new NetDataReader();
            cachedWriter = new NetDataWriter();

            connectionTracker.ConnectionAdded += OnConnectionStarted;
            connectionTracker.ConnectionRemoved += OnConnectionClosed;
        }

        protected virtual void OnDestroy()
        {
            connectionTracker.ConnectionAdded -= OnConnectionStarted;
            connectionTracker.ConnectionRemoved -= OnConnectionClosed;
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

        protected void WritePacketHandlerDataToCachedWriter(IPacketHandler handler, NetDataWriter writer)
        {
            cachedWriter.Reset();

            cachedWriter.Put(handler.HandlerId);
            cachedWriter.Put(writer.Data, 0, writer.Length);
        }

        protected void NotifyPacketHandlers_NetworkStarted()
        {
            if (hasNotifiedPacketHandlersOfStart)
            {
                return;
            }

            hasNotifiedPacketHandlersOfStart = true;

            foreach (var (_, packetHandler) in packetHandlers)
            {
                packetHandler.OnNetworkStarted(this);
            }
        }

        protected void NotifyPacketHandlers_NetworkStopped()
        {
            if (!hasNotifiedPacketHandlersOfStart)
            {
                return;
            }

            hasNotifiedPacketHandlersOfStart = false;

            foreach (var (_, packetHandler) in packetHandlers)
            {
                packetHandler.OnNetworkStopped(this);
            }
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
            transport.ConnectionStarted += Transport_OnConnectionStarted;
            transport.ConnectionStopped += Transport_OnConnectionStopped;
            transport.ReceivedData += Transport_OnReceivedData;
        }

        protected void UnregisterTransportEvents(ITransport transport)
        {
            transport.ReceivedData -= Transport_OnReceivedData;
            transport.ConnectionStopped += Transport_OnConnectionStopped;
            transport.ConnectionStarted += Transport_OnConnectionStarted;
        }

        protected virtual void OnTickTransports() {}

        private void OnConnectionStarted(NetworkConnection connection)
        {
            ConnectionStarted?.Invoke(this, connection);
        }

        private void OnConnectionClosed(NetworkConnection connection)
        {
            ConnectionStopped?.Invoke(this, connection);
        }

        private void Transport_OnConnectionStarted(ITransport transport, int transportConnectionId)
        {
            connectionTracker.AddNetworkConnection(transport, transportConnectionId);
        }

        private void Transport_OnConnectionStopped(ITransport transport, int transportConnectionId)
        {
            connectionTracker.RemoveNetworkConnection(transport, transportConnectionId);
        }

        private void Transport_OnReceivedData(ITransport transport, int transportConnectionId, ArraySegment<byte> data, SendType sendType)
        {
            var connection = connectionTracker.GetNetworkConnection(transport, transportConnectionId);
            if (connection == null)
            {
                return;
            }

            cachedReader.SetSource(data.Array, data.Offset, data.Offset + data.Count);

            var packetHandlerId = cachedReader.GetInt();
            if (!packetHandlers.TryGetValue(packetHandlerId, out var packetHandler))
            {
                return;
            }

            packetHandler.OnReceive(this, connection, cachedReader, sendType);
        }
    }
}
