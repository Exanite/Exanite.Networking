namespace Exanite.Networking.Transports
{
    public interface ITransport
    {
        public void Tick();
    }

    public interface ITransport<out TServer, out TClient> : ITransport
        where TServer : ITransportServer
        where TClient : ITransportClient
    {
        public TServer Server { get; }
        public TClient Client { get; }

        void ITransport.Tick()
        {
            Server.Tick();
            Client.Tick();
        }
    }

    public interface ITransportServer
    {
        public void Tick();
    }

    public interface ITransportClient
    {
        public void Tick();
    }
}
