using System;
using System.Collections.Generic;
using Exanite.Networking.Transports;
using LiteNetLib.Utils;
using UnityEngine;

namespace Exanite.Networking
{
    public abstract class Network : MonoBehaviour
    {
        protected Dictionary<int, IPacketHandler> packetHandlers;
        protected NetDataWriter cachedWriter;

        public IReadOnlyDictionary<int, IPacketHandler> PacketHandlers => packetHandlers;

        public abstract bool IsReady { get; }

        protected void Awake()
        {
            packetHandlers = new Dictionary<int, IPacketHandler>();
            cachedWriter = new NetDataWriter();
        }

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
            connection.Transport.SendData(connection.TransportConnectionId, writer, sendType);
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
    }

    public class NetworkServer : Network
    {
        private readonly Dictionary<int, NetworkConnection> connections = new();

        public IReadOnlyDictionary<int, NetworkConnection> Connections => connections;

        public override bool IsReady => throw new NotImplementedException();
    }

    public class NetworkClient : Network
    {
        public ITransportClient SelectedTransport { get; private set; }

        public NetworkConnection Server { get; private set; }

        public void SetTransport(ITransportClient transport)
        {
            SelectedTransport = transport;
        }

        public override bool IsReady => throw new NotImplementedException();
    }
}
