using LiteNetLib.Utils;

namespace Exanite.Networking.Channels
{
    public class NetworkProtocolChannel : INetworkSerializable
    {
        public string Key { get; set; }
        public string PacketType { get; set; }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Key);
            writer.Put(PacketType);
        }

        public void Deserialize(NetDataReader reader)
        {
            Key = reader.GetString();
            PacketType = reader.GetString();
        }
    }
}
