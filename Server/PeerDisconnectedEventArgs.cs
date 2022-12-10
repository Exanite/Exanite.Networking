﻿using System;
using LiteNetLib;

namespace Exanite.Networking.Server
{
    public class PeerDisconnectedEventArgs : EventArgs
    {
        public PeerDisconnectedEventArgs(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Peer = peer;
            DisconnectInfo = disconnectInfo;
        }

        public NetPeer Peer { get; }
        public DisconnectInfo DisconnectInfo { get; }
    }
}