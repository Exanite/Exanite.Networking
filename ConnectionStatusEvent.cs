namespace Exanite.Networking
{
    public delegate void ConnectionStatusEvent(INetwork network, NetworkConnection connection, RemoteConnectionStatus status);

    public readonly struct ConnectionStatusEventArgs
    {
        public ConnectionStatusEventArgs(NetworkConnection connection, RemoteConnectionStatus status)
        {
            Connection = connection;
            Status = status;
        }

        public NetworkConnection Connection { get; }
        public RemoteConnectionStatus Status { get; }
    }
}
