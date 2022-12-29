using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Exanite.Networking.Transports;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace Exanite.Networking
{
    public class NetworkServer : Network, INetworkServer
    {
        [Required] [OdinSerialize] private List<ITransportServer> transports = new();

        public IReadOnlyList<ITransportServer> Transports => transports;

        public override async UniTask StartConnection()
        {
            ValidateIsStopped();

            Status = LocalConnectionStatus.Starting;

            try
            {
                foreach (var transport in transports)
                {
                    RegisterTransportEvents(transport);

                    await transport.StartConnection();
                }
            }
            catch (Exception e)
            {
                StopConnection();

                throw new NetworkException($"Exception thrown while starting {GetType().Name}", e);
            }

            Status = LocalConnectionStatus.Started;

            NotifyPacketHandlers_NetworkStarted();
        }

        public override void StopConnection()
        {
            foreach (var transport in transports)
            {
                transport.StopConnection();

                UnregisterTransportEvents(transport);
            }

            Status = LocalConnectionStatus.Stopped;

            NotifyPacketHandlers_NetworkStopped();
        }

        protected override bool AreTransportsAllStarted()
        {
            foreach (var transport in transports)
            {
                if (transport.Status != LocalConnectionStatus.Started)
                {
                    return false;
                }
            }

            return true;
        }

        protected override void OnTickTransports()
        {
            base.OnTickTransports();

            foreach (var transport in transports)
            {
                transport.Tick();
            }
        }
    }
}
