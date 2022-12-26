using Exanite.Networking.Transports;

namespace Exanite.Networking
{
    public class ConnectionFactory
    {
        private int nextConnectionId;

        public NetworkConnection CreateNetworkConnection(ITransport transport, int transportConnectionId)
        {
            return new NetworkConnection(nextConnectionId++, transport, transportConnectionId);
        }
    }
}
