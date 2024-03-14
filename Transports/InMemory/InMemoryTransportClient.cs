using Cysharp.Threading.Tasks;

namespace Exanite.Networking.Transports.InMemory
{
    public class InMemoryTransportClient : InMemoryTransport, ITransportClient
    {
        public InMemoryTransportClient(InMemoryTransportSettings settings) : base(settings) {}

        public override async UniTask StartConnection()
        {
            // Prevent one frame delay issues when both server and client are started at the same time.
            await UniTask.Yield();

            if (!InMemoryTransportServer.Servers.TryGetValue(Settings.VirtualPort, out var server))
            {
                throw new NetworkException($"No {typeof(InMemoryTransportServer).Name} active on virtual port {Settings.VirtualPort}.");
            }

            server.OnClientConnected(this);

            Status = LocalConnectionStatus.Started;
        }
    }
}
