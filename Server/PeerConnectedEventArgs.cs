using System;
using LiteNetLib;

namespace Exanite.Networking.Server
{
    public class PeerConnectedEventArgs : EventArgs
    {
        public PeerConnectedEventArgs(NetPeer peer)
        {
            Peer = peer;
        }

        public NetPeer Peer { get; }
    }
}