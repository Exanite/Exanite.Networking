namespace Exanite.Networking.Channels
{
    public interface INetworkChannel<TMessage> where TMessage : INetworkSerializable
    {
        public string Key { get; }

        public bool IsReady { get; }

        public TMessage Message { get; set; }

        public INetwork Network { get; }

        public void Send(NetworkConnection connection);
        public void Send(NetworkConnection connection, TMessage message);

        public void Write();
        public void Write(TMessage message);

        public void SendNoWrite(NetworkConnection connection);

        public void RegisterHandler(MessageHandler<TMessage> handler);
    }
}
