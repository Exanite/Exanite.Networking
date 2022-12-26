using System.Collections.Generic;
using Exanite.Networking.Transports;

namespace Exanite.Networking
{
    public class ConnectionTracker
    {
        private Dictionary<int, NetworkConnection> connections = new();
        private readonly Dictionary<ITransport, Dictionary<int, NetworkConnection>> connectionLookUp = new();

        public IReadOnlyDictionary<int, NetworkConnection> Connections => connections;

        public NetworkConnection GetNetworkConnection(ITransport transport, int transportConnectionId)
        {
            if (connectionLookUp.TryGetValue(transport, out var transportConnections)
                && transportConnections.TryGetValue(transportConnectionId, out var connection))
            {
                return connection;
            }

            return null;
        }
    }
}
