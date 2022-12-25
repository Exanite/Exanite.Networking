using System;
using LiteNetLib;

namespace Exanite.Networking.Client
{
    public class ClientConnectedEventArgs : EventArgs
    {
        public ClientConnectedEventArgs(NetPeer server)
        {
            Server = server;
        }

        public NetPeer Server { get; }
    }
}
