using System;
using System.Collections.Generic;
using System.Net;
using Cysharp.Threading.Tasks;
using Exanite.Core.Events;
using Exanite.Networking.Client;
using Exanite.Networking.Server;
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

    public class LnlTransportClient : LnlTransport
    {
        private DisconnectInfo previousDisconnectInfo;

        public NetPeer Server { get; private set; }

        public LocalConnectionStatus Status { get; private set; }
        public override bool IsReady => Status == LocalConnectionStatus.Started;

        public event EventHandler<LnlTransportClient, ClientConnectedEventArgs> Connected;
        public event EventHandler<LnlTransportClient, ClientDisconnectedEventArgs> Disconnected;

        protected override void OnDestroy()
        {
            StopConnection(false);

            base.OnDestroy();
        }

        public async UniTask<ClientConnectResult> StartConnection(IPEndPoint endPoint)
        {
            switch (Status)
            {
                case LocalConnectionStatus.Starting: throw new InvalidOperationException("Client is already connecting.");
                case LocalConnectionStatus.Started: throw new InvalidOperationException("Client is already connected.");
            }

            Status = LocalConnectionStatus.Starting;

            netManager.Start();
            netManager.Connect(endPoint, ConnectionKey);

            await UniTask.WaitUntil(() => Status != LocalConnectionStatus.Starting);

            return new ClientConnectResult(Status == LocalConnectionStatus.Started, previousDisconnectInfo.Reason.ToString());
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

        protected override void OnPeerConnected(NetPeer server)
        {
            Connected?.Invoke(this, new ClientConnectedEventArgs(server));

            Status = LocalConnectionStatus.Started;

            Server = server;
        }

        protected override void OnPeerDisconnected(NetPeer server, DisconnectInfo disconnectInfo)
        {
            if (Status == LocalConnectionStatus.Started)
            {
                Disconnected?.Invoke(this, new ClientDisconnectedEventArgs(server, disconnectInfo));
            }

            Status = LocalConnectionStatus.Stopped;

            Server = null;
            previousDisconnectInfo = disconnectInfo;
        }

        protected override void OnConnectionRequest(ConnectionRequest request)
        {
            request.Reject();
        }
    }

    public class LnlTransportServer : LnlTransport
    {
        private readonly List<NetPeer> connectedPeers = new();

        public IReadOnlyList<NetPeer> ConnectedPeers => connectedPeers;

        public bool IsCreated { get; private set; }
        public override bool IsReady => IsCreated;

        public event EventHandler<LnlTransportServer, PeerConnectedEventArgs> PeerConnected;
        public event EventHandler<LnlTransportServer, PeerDisconnectedEventArgs> PeerDisconnected;

        protected override void OnDestroy()
        {
            Close(false);

            base.OnDestroy();
        }

        public void Create(int port)
        {
            if (IsCreated)
            {
                throw new InvalidOperationException("Server has already been created.");
            }

            netManager.Start(port);

            IsCreated = true;
        }

        public void Close()
        {
            Close(true);
        }

        public void SendAsPacketHandlerToAll(IPacketHandler handler, NetDataWriter writer, DeliveryMethod deliveryMethod)
        {
            ValidateIsReadyToSend();

            WritePacketHandlerDataToCachedWriter(handler, writer);
            netManager.SendToAll(cachedWriter, deliveryMethod);
        }

        public void DisconnectPeer(NetPeer peer)
        {
            netManager.DisconnectPeer(peer);
        }

        protected void Close(bool pollEvents)
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

        protected override void OnPeerConnected(NetPeer peer)
        {
            connectedPeers.Add(peer);

            PeerConnected?.Invoke(this, new PeerConnectedEventArgs(peer));
        }

        protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            connectedPeers.Remove(peer);

            PeerDisconnected?.Invoke(this, new PeerDisconnectedEventArgs(peer, disconnectInfo));
        }

        protected override void OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey(ConnectionKey);
        }
    }
}
