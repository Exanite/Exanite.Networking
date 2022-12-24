using LiteNetLib;

namespace Exanite.Networking
{
    public interface IPacketHandler
    {
        int HandlerId { get; }

        void Receive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod);
    }
}
