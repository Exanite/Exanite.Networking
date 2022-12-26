using LiteNetLib.Utils;

namespace Exanite.Networking
{
    public interface IPacketHandler
    {
        int HandlerId { get; }

        void OnReceive(NetworkConnection connection, NetDataReader reader, SendType sendType);
    }
}
