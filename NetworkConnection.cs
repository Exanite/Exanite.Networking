using Exanite.Networking.Transports;

namespace Exanite.Networking
{
    public class NetworkConnection
    {
        public NetworkConnection(int id, ITransport transport, int transportConnectionId)
        {
            Id = id;

            Transport = transport;
            TransportConnectionId = transportConnectionId;
        }

        public int Id { get; }

        public ITransport Transport { get; }
        public int TransportConnectionId { get; }

        public RemoteConnectionStatus Status => Transport.GetConnectionStatus(this);
    }
}
