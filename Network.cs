using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Exanite.Networking.Transports;
using LiteNetLib.Utils;
using UnityEngine;

namespace Exanite.Networking
{
    public abstract class Network : MonoBehaviour
    {
        protected ConnectionTracker connectionTracker;
        protected Dictionary<int, IPacketHandler> packetHandlers;

        protected NetDataReader cachedReader;
        protected NetDataWriter cachedWriter;

        private int nextConnectionId;

        public LocalConnectionStatus Status { get; protected set; }
        public virtual bool IsReady => Status == LocalConnectionStatus.Started;

        public IReadOnlyDictionary<int, IPacketHandler> PacketHandlers => packetHandlers;
        public IReadOnlyDictionary<int, NetworkConnection> Connections => connectionTracker.Connections;

        // Todo Need to guarantee connection/disconnect events are called in pairs
        public event ConnectionStartedEvent ConnectionStarted;
        public event ConnectionStoppedEvent ConnectionStopped;

        protected void Awake()
        {
            connectionTracker = new ConnectionTracker();
            packetHandlers = new Dictionary<int, IPacketHandler>();

            cachedReader = new NetDataReader();
            cachedWriter = new NetDataWriter();
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

            var data = new ArraySegment<byte>(writer.Data, 0, writer.Length);
            connection.Transport.SendData(connection.TransportConnectionId, data, sendType);
        }

        protected void WritePacketHandlerDataToCachedWriter(IPacketHandler handler, NetDataWriter writer)
        {
            cachedWriter.Reset();

            cachedWriter.Put(handler.HandlerId);
            cachedWriter.Put(writer.Data, 0, writer.Length);
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

        protected virtual NetworkConnection AddNetworkConnection(ITransport transport, int transportConnectionId)
        {
            var connection = new NetworkConnection(nextConnectionId, transport, transportConnectionId);

            nextConnectionId++;

            return connection;
        }

        protected virtual void RemoveNetworkConnection(NetworkConnection connection)
        {

        }

        protected virtual void Transport_OnConnectionStarted(ITransport transport, int transportConnectionId)
        {
            AddNetworkConnection(transport, transportConnectionId);
        }

        protected virtual void Transport_OnConnectionStopped(ITransport transport, int transportConnectionId)
        {
            var connection = connectionTracker.GetNetworkConnection(transport, transportConnectionId);
            RemoveNetworkConnection(connection);
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

            packetHandler.OnReceive(connection, cachedReader, sendType);
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
