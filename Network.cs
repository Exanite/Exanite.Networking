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
        protected ConnectionTracker connectionTracker;
        protected Dictionary<int, IPacketHandler> packetHandlers;

        protected NetDataReader cachedReader;
        protected NetDataWriter cachedWriter;

        private ConnectionFactory connectionFactory;
        private bool hasNotifiedPacketHandlersOfStart;

        public LocalConnectionStatus Status { get; protected set; }
        public virtual bool IsReady => Status == LocalConnectionStatus.Started;

        public IReadOnlyDictionary<int, IPacketHandler> PacketHandlers => packetHandlers;
        public IReadOnlyDictionary<int, NetworkConnection> Connections => connectionTracker.Connections;

        public event ConnectionStartedEvent ConnectionStarted;
        public event ConnectionStoppedEvent ConnectionStopped;

        protected virtual void Awake()
        {
            connectionFactory = new ConnectionFactory();
            connectionTracker = new ConnectionTracker(connectionFactory);
            packetHandlers = new Dictionary<int, IPacketHandler>();

            cachedReader = new NetDataReader();
            cachedWriter = new NetDataWriter();

            connectionTracker.ConnectionAdded += OnConnectionAdded;
            connectionTracker.ConnectionRemoved += OnConnectionRemoved;
        }

        protected virtual void OnDestroy()
        {
            connectionTracker.ConnectionAdded -= OnConnectionAdded;
            connectionTracker.ConnectionRemoved -= OnConnectionRemoved;
        }

        protected virtual void FixedUpdate()
        {
            if (Status == LocalConnectionStatus.Started && !AreTransportsAllStarted())
            {
                StopConnection();
            }

            Tick();
        }

        public virtual void Tick() {}

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

            var data = new ArraySegment<byte>(writer.Data, 0, writer.Length);
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

        protected virtual void OnConnectionAdded(NetworkConnection connection)
        {
            ConnectionStarted?.Invoke(this, connection);
        }

        protected virtual void OnConnectionRemoved(NetworkConnection connection)
        {
            ConnectionStopped?.Invoke(this, connection);
        }

        protected virtual void Transport_OnConnectionStarted(ITransport transport, int transportConnectionId)
        {
            connectionTracker.AddNetworkConnection(transport, transportConnectionId);
        }

        protected virtual void Transport_OnConnectionStopped(ITransport transport, int transportConnectionId)
        {
            connectionTracker.RemoveNetworkConnection(transport, transportConnectionId);
        }

        protected virtual void Transport_OnReceivedData(ITransport transport, int transportConnectionId, ArraySegment<byte> data, SendType sendType)
        {
            var connection = connectionTracker.GetNetworkConnection(transport, transportConnectionId);
            if (connection == null)
            {
                return;
            }

            // Todo Check for accuracy, not sure what maxSize is
            cachedReader.SetSource(data.Array, data.Offset, data.Offset + data.Count);

            var packetHandlerId = cachedReader.GetInt();

            if (!packetHandlers.TryGetValue(packetHandlerId, out var packetHandler))
            {
                return;
            }

            packetHandler.OnReceive(this, connection, cachedReader, sendType);
        }

        protected virtual void RegisterTransportEvents(ITransport transport)
        {
            transport.ConnectionStarted += Transport_OnConnectionStarted;
            transport.ConnectionStopped += Transport_OnConnectionStopped;
            transport.ReceivedData += Transport_OnReceivedData;
        }

        protected virtual void UnregisterTransportEvents(ITransport transport)
        {
            transport.ReceivedData -= Transport_OnReceivedData;
            transport.ConnectionStopped += Transport_OnConnectionStopped;
            transport.ConnectionStarted += Transport_OnConnectionStarted;
        }
    }
}
