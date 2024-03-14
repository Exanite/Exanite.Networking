using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Exanite.Core.Events;
using LiteNetLib;

namespace Exanite.Networking.Transports.LiteNetLib
{
    public class LnlTransport : ITransport
    {
        protected EventBasedNetListener listener;
        protected NetManager netManager;

        protected Dictionary<int, NetPeer> connections;

        public LnlTransportSettings Settings { get; }

        public INetwork Network { get; set; }
        public LocalConnectionStatus Status { get; protected set; }

        public event EventHandler<ITransport, TransportDataReceivedEventArgs> DataReceived;
        public event EventHandler<ITransport, TransportConnectionStatusEventArgs> ConnectionStatus;

        public LnlTransport(LnlTransportSettings settings)
        {
            Settings = settings;

            listener = new EventBasedNetListener();
            netManager = new NetManager(listener);

            connections = new Dictionary<int, NetPeer>();

            listener.PeerConnectedEvent += OnPeerConnected;
            listener.PeerDisconnectedEvent += OnPeerDisconnected;
            listener.NetworkReceiveEvent += OnNetworkReceive;
            listener.ConnectionRequestEvent += OnConnectionRequest;
        }

        public void Dispose()
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

        public async UniTask StartConnection()
        {
            if (Network.IsServer)
            {
                netManager.Start(Settings.Port);

                Status = LocalConnectionStatus.Started;
            }

            if (Network.IsClient)
            {
                Status = LocalConnectionStatus.Starting;

                netManager.Start();
                netManager.Connect(Settings.RemoteAddress, Settings.Port, Settings.ConnectionKey);

                await UniTask.WaitUntil(() => Status != LocalConnectionStatus.Starting);

                if (Status != LocalConnectionStatus.Started)
                {
                    throw new NetworkException("Failed to connect.");
                }
            }
        }

        public void StopConnection()
        {
            StopConnection(true);
        }

        protected void StopConnection(bool handleEvents)
        {
            try
            {
                netManager.DisconnectAll();

                if (handleEvents)
                {
                    netManager.PollEvents();
                }

                netManager.Stop();
            }
            finally
            {
                connections.Clear();

                Status = LocalConnectionStatus.Stopped;
            }
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
                throw new NetworkException("Attempted to send data to invalid connection.");
            }

            peer.Send(data.Array, data.Offset, data.Count, sendType.ToDeliveryMethod());
        }

        protected virtual void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            var data = new ArraySegment<byte>(reader.RawData, reader.Position, reader.AvailableBytes);
            DataReceived?.Invoke(this, new TransportDataReceivedEventArgs(peer.Id, data, deliveryMethod.ToSendType()));
        }

        protected virtual void OnPeerConnected(NetPeer peer)
        {
            if (Network.IsClient)
            {
                Status = LocalConnectionStatus.Started;
            }

            connections.Add(peer.Id, peer);

            ConnectionStatus?.Invoke(this, new TransportConnectionStatusEventArgs(peer.Id, RemoteConnectionStatus.Started));
        }

        protected virtual void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (Network.IsClient)
            {
                Status = LocalConnectionStatus.Stopped;
            }

            if (connections.Remove(peer.Id))
            {
                ConnectionStatus?.Invoke(this, new TransportConnectionStatusEventArgs(peer.Id, RemoteConnectionStatus.Stopped));
            }
        }

        protected void OnConnectionRequest(ConnectionRequest request)
        {
            if (Network.IsServer)
            {
                request.AcceptIfKey(Settings.ConnectionKey);
            }

            if (Network.IsClient)
            {
                request.Reject();
            }
        }
    }
}
