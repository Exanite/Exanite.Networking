namespace Exanite.Networking.Channels
{
    public delegate void MessageHandler<in TMessage>(NetworkConnection connection, TMessage message) where TMessage : INetworkSerializable;
}
