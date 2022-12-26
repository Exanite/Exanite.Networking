using Exanite.Networking.Transports;

namespace Exanite.Networking
{
    public interface INetworkClient : INetwork
    {
        public ITransportClient Transport { get; }
        public NetworkConnection ServerConnection { get; }
    }
}
