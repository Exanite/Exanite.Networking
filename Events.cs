namespace Exanite.Networking
{
    public delegate void ConnectionStartedEvent(INetwork network, NetworkConnection connection);

    public delegate void ConnectionStoppedEvent(INetwork network, NetworkConnection connection);

    public delegate void NetworkStartedEvent(INetwork network);

    public delegate void NetworkStoppedEvent(INetwork network);
}
