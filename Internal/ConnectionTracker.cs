using System;
using System.Collections.Generic;
using System.Linq;
using Exanite.Networking.Transports;

namespace Exanite.Networking.Internal
{
    public class ConnectionTracker
    {
        private readonly Dictionary<int, NetworkConnection> connections = new();
        private readonly Dictionary<ITransport, Dictionary<int, NetworkConnection>> connectionLookup = new();

        private readonly ConnectionFactory connectionFactory;

        public ConnectionTracker(ConnectionFactory connectionFactory)
        {
            this.connectionFactory = connectionFactory;
        }

        public IReadOnlyDictionary<int, NetworkConnection> Connections => connections;

        public event Action<NetworkConnection> ConnectionAdded;
        public event Action<NetworkConnection> ConnectionRemoved;

        public NetworkConnection GetNetworkConnection(ITransport transport, int transportConnectionId)
        {
            if (connectionLookup.TryGetValue(transport, out var transportConnections)
                && transportConnections.TryGetValue(transportConnectionId, out var connection))
            {
                return connection;
            }

            return null;
        }

        public NetworkConnection AddNetworkConnection(ITransport transport, int transportConnectionId)
        {
            var connection = connectionFactory.CreateNetworkConnection(transport, transportConnectionId);

            connections.Add(connection.Id, connection);
            AddToLookup(connection);

            ConnectionAdded?.Invoke(connection);

            return connection;
        }

        public bool RemoveNetworkConnection(ITransport transport, int transportConnectionId)
        {
            var connection = GetNetworkConnection(transport, transportConnectionId);
            if (connection == null)
            {
                return false;
            }

            return RemoveNetworkConnection(connection.Id);
        }

        public bool RemoveNetworkConnection(int connectionId)
        {
            if (connections.TryGetValue(connectionId, out var connection))
            {
                connections.Remove(connectionId);
                RemoveFromLookup(connection);

                ConnectionRemoved?.Invoke(connection);

                return true;
            }

            return false;
        }

        public void Clear()
        {
            connectionLookup.Clear();

            var idsToRemove = connections.Keys.ToList();
            foreach (var connectionId in idsToRemove)
            {
                RemoveNetworkConnection(connectionId);
            }
        }

        private void AddToLookup(NetworkConnection connection)
        {
            if (!connectionLookup.TryGetValue(connection.Transport, out var transportConnections))
            {
                transportConnections = new Dictionary<int, NetworkConnection>();
                connectionLookup.Add(connection.Transport, transportConnections);
            }

            transportConnections.Add(connection.TransportConnectionId, connection);
        }

        private void RemoveFromLookup(NetworkConnection connection)
        {
            if (!connectionLookup.TryGetValue(connection.Transport, out var transportConnections))
            {
                return;
            }

            transportConnections.Remove(connection.TransportConnectionId);
        }
    }
}
