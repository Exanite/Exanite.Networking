using LiteNetLib;

namespace Exanite.Networking
{
    public interface IPacketHandler
    {
        int HandlerId { get; }

        void OnReceive(NetPeer peer, NetPacketReader reader, SendType sendType);
    }
}
