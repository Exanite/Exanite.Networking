using System;
using LiteNetLib;

namespace Exanite.Networking.Client
{
    public class ClientDisconnectedEventArgs : EventArgs
    {
        public ClientDisconnectedEventArgs(NetPeer server, DisconnectInfo disconnectInfo)
        {
            Server = server;
            DisconnectInfo = disconnectInfo;
        }

        public NetPeer Server { get; }
        public DisconnectInfo DisconnectInfo { get; }
    }
}
