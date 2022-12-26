using System;
using System.Collections.Generic;
using System.Net;
using Cysharp.Threading.Tasks;
using LiteNetLib;
using UnityEngine;

namespace Exanite.Networking.Transports.LiteNetLib
{
    public abstract class LnlTransport : MonoBehaviour, ITransport
    {
        [SerializeField] protected string connectionKey = Constants.DefaultConnectionKey;

        protected EventBasedNetListener listener;
        protected NetManager netManager;

        public string ConnectionKey
        {
            get => connectionKey;
            set => connectionKey = value;
        }

        public LocalConnectionStatus Status { get; protected set; }

        public event TransportReceivedDataEvent ReceivedData;
        public event TransportConnectionStartedEvent ConnectionStarted;
        public event TransportConnectionStartedEvent ConnectionStopped;

        protected virtual void Awake()
        {
            listener = new EventBasedNetListener();
            netManager = new NetManager(listener);

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

        public void Tick()
        {
            netManager.PollEvents();
        }

        public UniTask StartConnection()
        {
            throw new NotImplementedException();
        }

        public void StopConnection()
        {
            throw new NotImplementedException();
        }

        public RemoteConnectionStatus GetConnectionStatus(NetworkConnection networkConnection)
        {
            throw new NotImplementedException();
        }

        public void SendData(int connectionId, ArraySegment<byte> data, SendType sendType)
        {
            // peer.Send(data.Array, data.Offset, data.Count, sendType.ToLnlDeliveryMethod());

            throw new NotImplementedException();
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

    public class LnlTransportClient : LnlTransport, ITransportClient
    {
        protected override void OnDestroy()
        {
            StopConnection(false);

            base.OnDestroy();
        }

        public async UniTask StartConnection(IPEndPoint endPoint)
        {
            Status = LocalConnectionStatus.Starting;

            netManager.Start();
            netManager.Connect(endPoint, ConnectionKey);

            await UniTask.WaitUntil(() => Status != LocalConnectionStatus.Starting);

            if (Status != LocalConnectionStatus.Started)
            {
                throw new Exception("Failed to connect.");
            }
        }

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

        protected override void OnPeerConnected(NetPeer peer)
        {
            Status = LocalConnectionStatus.Started;

            base.OnPeerConnected(peer);
        }

        protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (Status == LocalConnectionStatus.Started)
            {
                Status = LocalConnectionStatus.Stopped;

                base.OnPeerDisconnected(peer, disconnectInfo);
            }

            Status = LocalConnectionStatus.Stopped;
        }

        protected override void OnConnectionRequest(ConnectionRequest request)
        {
            request.Reject();
        }
    }

    public class LnlTransportServer : LnlTransport, ITransportServer
    {
        private readonly List<NetPeer> connectedPeers = new();

        public IReadOnlyList<NetPeer> ConnectedPeers => connectedPeers;

        public bool IsCreated { get; private set; }

        protected override void OnDestroy()
        {
            StopConnection(false);

            base.OnDestroy();
        }

        public void StartConnection(int port)
        {
            if (IsCreated)
            {
                throw new InvalidOperationException("Server has already been created.");
            }

            netManager.Start(port);

            IsCreated = true;
        }

        public void StopConnection()
        {
            StopConnection(true);
        }

        public void DisconnectPeer(NetPeer peer)
        {
            netManager.DisconnectPeer(peer);
        }

        protected void StopConnection(bool pollEvents)
        {
            if (!IsCreated)
            {
                return;
            }

            netManager.DisconnectAll();

            if (pollEvents)
            {
                netManager.PollEvents();
            }

            netManager.Stop();

            IsCreated = false;
        }

        protected override void OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey(ConnectionKey);
        }
    }
}
