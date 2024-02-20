using Exanite.Networking.Transports;

namespace Exanite.Networking.Internal
{
    public class ConnectionFactory
    {
        private int nextConnectionId;

        public NetworkConnection CreateNetworkConnection(INetwork network, ITransport transport, int transportConnectionId)
        {
            return new NetworkConnection(network, nextConnectionId++, transport, transportConnectionId);
        }
    }
}
