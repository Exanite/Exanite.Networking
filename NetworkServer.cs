using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Exanite.Networking.Transports;
using Sirenix.Serialization;

namespace Exanite.Networking
{
    public class NetworkServer : Network
    {
        [OdinSerialize] private List<ITransportServer> transports = new();

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

                throw new Exception($"Exception thrown while starting {GetType().Name}", e);
            }

            Status = LocalConnectionStatus.Started;
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
    }
}
