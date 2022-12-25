namespace Exanite.Networking.Transports
{
    public interface ITransport<out TServer, out TClient>
        where TServer : ITransportServer
        where TClient : ITransportClient
    {
        public TServer Server { get; }
        public TClient Client { get; }

        public virtual void Tick()
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
