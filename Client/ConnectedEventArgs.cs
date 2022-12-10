using System;
using LiteNetLib;

namespace Exanite.Networking.Client
{
    public class ConnectedEventArgs : EventArgs
    {
        public ConnectedEventArgs(NetPeer server)
        {
            Server = server;
        }

        public NetPeer Server { get; }
    }
}