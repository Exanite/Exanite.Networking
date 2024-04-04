using Exanite.Networking.Transports;

namespace Exanite.Networking
{
    public class NetworkConnection
    {
        public NetworkConnection(INetwork network, int id, ITransport transport, int transportConnectionId)
        {
            Network = network;
            Id = id;

            Transport = transport;
            TransportConnectionId = transportConnectionId;
        }

        public INetwork Network { get; }
        public int Id { get; }

        public ITransport Transport { get; }
        public int TransportConnectionId { get; }

        public RemoteConnectionStatus Status => Transport.GetConnectionStatus(TransportConnectionId);

        /// <summary>
        /// Returns the MTU in bytes for this connection and send type.
        /// </summary>
        /// <remarks>
        /// Behavior is undefined for invalid connections.
        /// </remarks>
        public int GetMtu(SendType sendType)
        {
            return Transport.GetMtu(TransportConnectionId, sendType);
        }

        public void Disconnect()
        {
            Transport.DisconnectConnection(TransportConnectionId);
        }
    }
}
