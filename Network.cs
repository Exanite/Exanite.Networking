using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Exanite.Networking.Transports;
using LiteNetLib.Utils;
using Sirenix.Serialization;
using UnityEngine;

namespace Exanite.Networking
{
    public abstract class Network : MonoBehaviour
    {
        protected Dictionary<int, IPacketHandler> packetHandlers;
        protected NetDataWriter cachedWriter;

        public IReadOnlyDictionary<int, IPacketHandler> PacketHandlers => packetHandlers;

        public abstract LocalConnectionStatus Status { get; }
        public virtual bool IsReady => Status == LocalConnectionStatus.Started;

        protected void Awake()
        {
            packetHandlers = new Dictionary<int, IPacketHandler>();
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
    }

    public class NetworkServer : Network
    {
        [OdinSerialize] private List<ITransportServer> transports = new();

        private LocalConnectionStatus status;
        private Dictionary<int, NetworkConnection> connections = new();

        public IReadOnlyList<ITransportServer> Transports => transports;
        public IReadOnlyDictionary<int, NetworkConnection> Connections => connections;

        public override LocalConnectionStatus Status => status;

        public override async UniTask StartConnection()
        {
            status = LocalConnectionStatus.Starting;

            try
            {
                foreach (var transport in transports)
                {
                    await transport.StartConnection();
                }
            }
            catch
            {
                StopConnection();
            }

            status = LocalConnectionStatus.Started;
        }

        public override void StopConnection()
        {
            foreach (var transport in transports)
            {
                transport.StopConnection();
            }

            status = LocalConnectionStatus.Stopped;
        }
    }

    public class NetworkClient : Network
    {
        [OdinSerialize] private ITransportClient transport;

        private LocalConnectionStatus status;

        public ITransportClient Transport => transport;

        public void SetTransport(ITransportClient transport)
        {
            this.transport = transport;
        }

        public override LocalConnectionStatus Status => status;

        public override async UniTask StartConnection()
        {
            status = LocalConnectionStatus.Starting;

            try
            {
                await transport.StartConnection();
            }
            catch
            {
                StopConnection();
            }

            status = LocalConnectionStatus.Started;
        }

        public override void StopConnection()
        {
            transport.StopConnection();

            status = LocalConnectionStatus.Stopped;
        }
    }
}
