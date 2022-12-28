using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LiteNetLib;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Exanite.Networking.Transports.LiteNetLib
{
    public abstract class LnlTransport : MonoBehaviour, ITransport
    {
        [Header("Dependencies")]
        [Required] [SerializeField] private LnlTransportSettings settings;

        protected EventBasedNetListener listener;
        protected NetManager netManager;

        protected Dictionary<int, NetPeer> connections;

        public LnlTransportSettings Settings => settings;

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

        public void DisconnectConnection(int connectionId)
        {
            if (connections.TryGetValue(connectionId, out var connection))
            {
                connection.Disconnect();
            }
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
            connections.Add(peer.Id, peer);

            ConnectionStarted?.Invoke(this, peer.Id);
        }

        protected virtual void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (connections.Remove(peer.Id))
            {
                ConnectionStopped?.Invoke(this, peer.Id);
            }
        }

        protected abstract void OnConnectionRequest(ConnectionRequest request);
    }
}
