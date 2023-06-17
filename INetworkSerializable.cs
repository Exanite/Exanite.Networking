using LiteNetLib.Utils;

namespace Exanite.Networking
{
    public interface INetworkSerializable
    {
        public void Serialize(NetDataWriter writer);

        public void Deserialize(NetDataReader reader);
    }
}
