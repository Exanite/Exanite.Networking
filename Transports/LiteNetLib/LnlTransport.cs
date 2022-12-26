using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LiteNetLib;
using UnityEngine;

namespace Exanite.Networking.Transports.LiteNetLib
{
    public abstract class LnlTransport : MonoBehaviour, ITransport
    {
        [SerializeField] protected string connectionKey = Constants.DefaultConnectionKey;
        [SerializeField] private string remoteAddress = Constants.DefaultRemoteAddress;
        [SerializeField] private ushort port = Constants.DefaultPort;

        protected EventBasedNetListener listener;
        protected NetManager netManager;

        protected Dictionary<int, NetPeer> connections;

        public string ConnectionKey
        {
            get => connectionKey;
            set => connectionKey = value;
        }

        public string RemoteAddress
        {
            get => remoteAddress;
            set => remoteAddress = value;
        }

        public ushort Port
        {
            get => port;
            set => port = value;
        }

        public LocalConnectionStatus Status { get; protected set; }

        public event TransportReceivedDataEvent ReceivedData;
        public event TransportConnectionStartedEvent ConnectionStarted;
        public event TransportConnectionStartedEvent ConnectionStopped;

        protected virtual void Awake()
        {
            listener = new EventBasedNetListener();
            netManager = new NetManager(listener);

            connections = new Dictionary<int, NetPeer>();

            listener.PeerConnectedEvent += OnPeerConnected;
            listener.PeerDisconnectedEvent += OnPeerDisconnected;
            listener.NetworkReceiveEvent += OnNetworkReceive;
            listener.ConnectionRequestEvent += OnConnectionRequest;
        }

        protected virtual void OnDestroy()
        {
            StopConnection(false);

            listener.ConnectionRequestEvent -= OnConnectionRequest;
            listener.NetworkReceiveEvent -= OnNetworkReceive;
            listener.PeerDisconnectedEvent -= OnPeerDisconnected;
            listener.PeerConnectedEvent -= OnPeerConnected;
        }

        public void Tick()
        {
            netManager.PollEvents();
        }

        public abstract UniTask StartConnection();

        public void StopConnection()
        {
            StopConnection(true);
        }

        protected void StopConnection(bool pollEvents)
        {
            netManager.DisconnectAll();

            if (pollEvents)
            {
                netManager.PollEvents();
            }

            netManager.Stop();

            Status = LocalConnectionStatus.Stopped;
        }

        public RemoteConnectionStatus GetConnectionStatus(int connectionId)
        {
            return connections.ContainsKey(connectionId) ? RemoteConnectionStatus.Started : RemoteConnectionStatus.Stopped;
        }

        public void SendData(int connectionId, ArraySegment<byte> data, SendType sendType)
        {
            if (!connections.TryGetValue(connectionId, out var peer))
            {
                return;
            }

            peer.Send(data.Array, data.Offset, data.Count, sendType.ToDeliveryMethod());
        }

        protected virtual void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            var data = new ArraySegment<byte>(reader.RawData, reader.Position, reader.AvailableBytes);
            ReceivedData?.Invoke(this, peer.Id, data, deliveryMethod.ToSendType());
        }

        protected virtual void OnPeerConnected(NetPeer peer)
        {
            ConnectionStarted?.Invoke(this, peer.Id);
        }

        protected virtual void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            ConnectionStopped?.Invoke(this, peer.Id);
        }

        protected abstract void OnConnectionRequest(ConnectionRequest request);
    }
}
