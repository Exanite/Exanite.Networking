namespace Exanite.Networking
{
    public struct ConnectionStatusEventArgs
    {
        public ConnectionStatusEventArgs(int connectionId, RemoteConnectionStatus status)
        {
            ConnectionId = connectionId;
            Status = status;
        }

        public int ConnectionId { get; }
        public RemoteConnectionStatus Status { get; }
    }
}
