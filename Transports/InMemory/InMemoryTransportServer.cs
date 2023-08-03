using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Exanite.Networking.Transports.InMemory
{
    public class InMemoryTransportServer : InMemoryTransport, ITransportServer
    {
        public static Dictionary<int, InMemoryTransportServer> Servers { get; } = new();

        public override UniTask StartConnection()
        {
            if (!Servers.TryAdd(Settings.VirtualPort, this))
            {
                throw new NetworkException($"Virtual port {Settings.VirtualPort} is already in use.");
            }

            Status = LocalConnectionStatus.Started;

            return UniTask.CompletedTask;
        }
    }
}
