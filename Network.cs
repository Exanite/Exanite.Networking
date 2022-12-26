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
        protected Dictionary<int, NetworkConnection> connections;
        protected Dictionary<int, IPacketHandler> packetHandlers;

        protected NetDataReader cachedReader;
        protected NetDataWriter cachedWriter;

        public LocalConnectionStatus Status { get; protected set; }
        public virtual bool IsReady => Status == LocalConnectionStatus.Started;

        public IReadOnlyDictionary<int, IPacketHandler> PacketHandlers => packetHandlers;
        public IReadOnlyDictionary<int, NetworkConnection> Connections => connections;

        protected void Awake()
        {
            connections = new Dictionary<int, NetworkConnection>();
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

        protected void OnReceivedData(ITransport transport, int transportConnectionId, ArraySegment<byte> data, SendType sendType)
        {
            // Todo Check for accuracy, not sure what maxSize is
            cachedReader.SetSource(data.Array, data.Offset, data.Offset + data.Count);

            var packetHandlerId = cachedReader.GetInt();

            if (!packetHandlers.TryGetValue(packetHandlerId, out var packetHandler))
            {
                return;
            }

            var connection = GetNetworkConnection(transport, transportConnectionId);
            if (connection == null)
            {
                return;
            }

            packetHandler.OnReceive(connection, cachedReader, sendType);
        }

        protected abstract NetworkConnection GetNetworkConnection(ITransport transport, int transportConnectionId);

        protected virtual void CleanUp()
        {
            connections.Clear();
        }
    }

    public class NetworkServer : Network
    {
        [OdinSerialize] private List<ITransportServer> transports = new();

        protected Dictionary<ITransport, Dictionary<int, NetworkConnection>> connectionLookUp;

        public IReadOnlyList<ITransportServer> Transports => transports;

        public override async UniTask StartConnection()
        {
            ValidateIsStopped();

            Status = LocalConnectionStatus.Starting;

            InitializeConnectionLookUp();

            try
            {
                foreach (var transport in transports)
                {
                    transport.ReceivedData += OnReceivedData;

                    await transport.StartConnection();
                }
            }
            catch (Exception e)
            {
                StopConnection();

                throw new Exception($"Exception thrown while starting {GetType().Name}", e);
            }

            Status = LocalConnectionStatus.Started;
        }

        public override void StopConnection()
        {
            foreach (var transport in transports)
            {
                transport.StopConnection();

                transport.ReceivedData -= OnReceivedData;
            }

            Status = LocalConnectionStatus.Stopped;
        }

        protected override NetworkConnection GetNetworkConnection(ITransport transport, int transportConnectionId)
        {
            if (connectionLookUp.TryGetValue(transport, out var transportConnections)
                && transportConnections.TryGetValue(transportConnectionId, out var connection))
            {
                return connection;
            }

            return null;
        }

        protected void InitializeConnectionLookUp()
        {
            connectionLookUp.Clear();

            foreach (var transport in transports)
            {
                connectionLookUp.Add(transport, new Dictionary<int, NetworkConnection>());
            }
        }

        protected override void CleanUp()
        {
            base.CleanUp();

            connectionLookUp.Clear();
        }
    }

    public class NetworkClient : Network
    {
        [OdinSerialize] private ITransportClient transport;

        private NetworkConnection server;

        public ITransportClient Transport => transport;

        public void SetTransport(ITransportClient transport)
        {
            this.transport = transport;
        }

        public override async UniTask StartConnection()
        {
            ValidateIsStopped();

            Status = LocalConnectionStatus.Starting;

            try
            {
                await transport.StartConnection();
            }
            catch
            {
                StopConnection();
            }

            Status = LocalConnectionStatus.Started;
        }

        public override void StopConnection()
        {
            transport.StopConnection();

            Status = LocalConnectionStatus.Stopped;
        }

        protected override NetworkConnection GetNetworkConnection(ITransport transport, int transportConnectionId)
        {
            return server;
        }

        protected override void CleanUp()
        {
            base.CleanUp();

            server = null;
        }
    }
}
