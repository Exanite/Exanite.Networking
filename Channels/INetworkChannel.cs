namespace Exanite.Networking.Channels
{
    public interface INetworkChannel
    {
        public string Key { get; }

        public bool IsReady { get; }

        public INetwork Network { get; }
    }

    public interface INetworkChannel<TMessage> : INetworkChannel where TMessage : INetworkSerializable
    {
        public TMessage Message { get; set; }

        public void SendAndWrite(NetworkConnection connection);
        public void SendAndWrite(NetworkConnection connection, TMessage message);

        public void Write();
        public void Write(TMessage message);

        public void SendNoWrite(NetworkConnection connection);

        public void RegisterHandler(MessageHandler<TMessage> handler);
    }
}
