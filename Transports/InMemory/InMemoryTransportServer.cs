using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Exanite.Core.Collections;

namespace Exanite.Networking.Transports.InMemory
{
    public class InMemoryTransportServer : InMemoryTransport, ITransportServer
    {
        public static TwoWayDictionary<int, InMemoryTransportServer> Servers { get; } = new();

        public override UniTask StartConnection()
        {
            if (!Servers.TryAdd(Settings.VirtualPort, this))
            {
                throw new NetworkException($"Virtual port {Settings.VirtualPort} is already in use.");
            }

            Status = LocalConnectionStatus.Started;

            return UniTask.CompletedTask;
        }

        protected override void StopConnection(bool handleEvents)
        {
            base.StopConnection(handleEvents);

            Servers.Inverse.Remove(this);
        }
    }
}
