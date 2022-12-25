using System;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Exanite.Networking.Transports.LiteNetLib
{
    public abstract class LnlTransport : MonoBehaviour
    {
        [SerializeField]
        protected string connectionKey = Constants.DefaultConnectionKey;

        protected EventBasedNetListener listener;
        protected NetManager netManager;
        protected Dictionary<int, IPacketHandler> packetHandlers;

        protected NetDataWriter cachedWriter;

        public string ConnectionKey
        {
            get => connectionKey;
            set => connectionKey = value;
        }

        public abstract bool IsReady { get; }
        public IReadOnlyDictionary<int, IPacketHandler> PacketHandlers => packetHandlers;

        protected virtual void Awake()
        {
            listener = new EventBasedNetListener();
            netManager = new NetManager(listener);
            packetHandlers = new Dictionary<int, IPacketHandler>();

            cachedWriter = new NetDataWriter();

            listener.PeerConnectedEvent += OnPeerConnected;
            listener.PeerDisconnectedEvent += OnPeerDisconnected;
            listener.NetworkReceiveEvent += OnNetworkReceive;
            listener.ConnectionRequestEvent += OnConnectionRequest;
        }

        protected virtual void OnDestroy()
        {
            listener.ConnectionRequestEvent -= OnConnectionRequest;
            listener.NetworkReceiveEvent -= OnNetworkReceive;
            listener.PeerDisconnectedEvent -= OnPeerDisconnected;
            listener.PeerConnectedEvent -= OnPeerConnected;
        }

        private void FixedUpdate()
        {
            netManager.PollEvents();
        }

        public void RegisterPacketHandler(IPacketHandler handler)
        {
            packetHandlers.Add(handler.HandlerId, handler);
        }

        public void UnregisterPacketHandler(IPacketHandler handler)
        {
            packetHandlers.Remove(handler.HandlerId);
        }

        public void SendAsPacketHandler(IPacketHandler handler, NetPeer peer, NetDataWriter writer, SendType sendType)
        {
            ValidateIsReadyToSend();

            WritePacketHandlerDataToCachedWriter(handler, writer);
            peer.Send(cachedWriter, sendType.ToLnlDeliveryMethod());
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

        protected virtual void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            var packetHandlerId = reader.GetInt();

            if (!packetHandlers.TryGetValue(packetHandlerId, out var packetHandler))
            {
                return;
            }

            packetHandler.OnReceive(peer, reader, deliveryMethod.ToDeliveryMethod());
        }

        protected abstract void OnPeerConnected(NetPeer peer);

        protected abstract void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo);

        protected abstract void OnConnectionRequest(ConnectionRequest request);
    }
}
