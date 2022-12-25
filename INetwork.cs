using LiteNetLib;
using LiteNetLib.Utils;

namespace Exanite.Networking
{
    public interface INetwork
    {
        public bool IsReady { get; }

        public void RegisterPacketHandler(IPacketHandler handler);
        public void UnregisterPacketHandler(IPacketHandler handler);

        public void SendAsPacketHandler(IPacketHandler handler, NetPeer peer, NetDataWriter writer, SendType sendType);
    }
}
