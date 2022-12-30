namespace Exanite.Networking.Transports
{
    public readonly struct TransportConnectionStatusEventArgs
    {
        public TransportConnectionStatusEventArgs(int connectionId, RemoteConnectionStatus status)
        {
            ConnectionId = connectionId;
            Status = status;
        }

        public int ConnectionId { get; }
        public RemoteConnectionStatus Status { get; }
    }
}
