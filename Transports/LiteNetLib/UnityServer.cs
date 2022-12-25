using System;
using System.Collections.Generic;
using Exanite.Core.Events;
using LiteNetLib;
using LiteNetLib.Utils;
using Exanite.Networking.Server;

namespace Exanite.Networking.Transports.LiteNetLib
{
    public class UnityServer : UnityNetwork
    {
        private readonly List<NetPeer> connectedPeers = new();

        public IReadOnlyList<NetPeer> ConnectedPeers => connectedPeers;

        public bool IsCreated { get; private set; }
        public override bool IsReady => IsCreated;

        public event EventHandler<UnityServer, PeerConnectedEventArgs> PeerConnected;
        public event EventHandler<UnityServer, PeerDisconnectedEventArgs> PeerDisconnected;

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

        public void SendAsPacketHandlerToAll(IPacketHandler handler, NetDataWriter writer, global::LiteNetLib.DeliveryMethod deliveryMethod)
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
