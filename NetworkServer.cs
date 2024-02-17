using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Exanite.Networking.Transports;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.Serialization;
#endif

namespace Exanite.Networking
{
    public class NetworkServer : Network
    {
#if ODIN_INSPECTOR
        [Required] [OdinSerialize]
#endif
        private List<ITransportServer> transports = new();

        public override bool IsServer => true;
        public IReadOnlyList<ITransportServer> Transports => transports;

        public override async UniTask StartConnection()
        {
            ValidateIsStopped();

            Status = LocalConnectionStatus.Starting;

            try
            {
                await UniTask.WhenAll(transports.Select(transport => StartTransport(transport)));
            }
            catch (Exception e)
            {
                StopConnection();

                throw new NetworkException($"Exception thrown while starting {GetType().Name}", e);
            }
        }

        public override void StopConnection()
        {
            foreach (var transport in transports)
            {
                transport.StopConnection();

                UnregisterTransportEvents(transport);
            }

            Status = LocalConnectionStatus.Stopped;
        }

        protected override bool AreAnyTransportsStopped()
        {
            foreach (var transport in transports)
            {
                if (transport.Status == LocalConnectionStatus.Stopped)
                {
                    return true;
                }
            }

            return false;
        }

        protected override void OnTickTransports()
        {
            base.OnTickTransports();

            foreach (var transport in transports)
            {
                transport.Tick();
            }
        }

        private async UniTask StartTransport(ITransportServer transport)
        {
            RegisterTransportEvents(transport);

            await transport.StartConnection();

            if (Status == LocalConnectionStatus.Stopped)
            {
                throw new NetworkException($"{GetType().Name} was stopped while starting transports");
            }

            // The Network is considered Started if one transport has started.
            // This is because one transport starting slowly should not block the others from communicating.
            Status = LocalConnectionStatus.Started;
        }
    }
}
