using System.Collections.Generic;
using System.Linq;
using LiteNetLib.Utils;

namespace Exanite.Networking.Channels
{
    public class NetworkProtocol : INetworkSerializable
    {
        public List<NetworkProtocolChannel> Channels { get; } = new();

        public bool IsCompatible(NetworkProtocol other)
        {
            if (Channels.Count != other.Channels.Count)
            {
                return false;
            }

            var keys = new HashSet<string>(Channels.Select(channel => channel.Key));
            foreach (var otherChannel in other.Channels)
            {
                if (!keys.Contains(otherChannel.Key))
                {
                    return false;
                }
            }

            return true;
        }

        public void Clear()
        {
            Channels.Clear();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.PutListWithCount(Channels);
        }

        public void Deserialize(NetDataReader reader)
        {
            reader.GetListWithCount(Channels);
        }
    }
}
