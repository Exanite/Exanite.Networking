using LiteNetLib.Utils;

namespace Exanite.Networking
{
    public interface IPacketHandler
    {
        int HandlerId { get; }

        void OnNetworkStarted(INetwork network);

        void OnReceive(INetwork network, NetworkConnection connection, NetDataReader reader, SendType sendType);

        void OnNetworkStopped(INetwork network);
    }
}
