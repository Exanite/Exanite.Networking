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

        public RemoteConnectionStatus Status => Transport.GetConnectionStatus(TransportConnectionId);

        public void Disconnect()
        {
            // Todo Prevent queued messages from being processed after disconnecting.
            // Disconnect does not prevent queued messages from being processed. This allows potentially invalid data to be processed by the user of this API.
            Transport.DisconnectConnection(TransportConnectionId);
        }
    }
}
