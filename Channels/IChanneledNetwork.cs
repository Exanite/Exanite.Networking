using System.Collections.Generic;

namespace Exanite.Networking.Channels
{
    public interface IChanneledNetwork : INetwork
    {
        public IReadOnlyDictionary<string, INetworkChannel> Channels { get; }

        public INetworkChannel<T> CreateChannel<T>(string key, T packet, SendType sendType) where T : INetworkSerializable;
    }
}
