using LiteNetLib.Utils;

namespace Exanite.Networking
{
    public interface ISimpleNetwork : INetwork
    {
        public event NetworkDataReceived NetworkDataReceived;

        public void Send(NetworkConnection connection, NetDataWriter writer, SendType sendType);
    }
}
