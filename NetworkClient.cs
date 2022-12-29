using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Exanite.Networking.Transports;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace Exanite.Networking
{
    public class NetworkClient : Network, INetworkClient
    {
        [Required] [OdinSerialize] private ITransportClient transport;

        public ITransportClient Transport => transport;
        public NetworkConnection ServerConnection => connectionTracker.Connections.Values.FirstOrDefault();

        public override void Tick()
        {
            base.Tick();

            transport.Tick();
        }

        public void SetTransport(ITransportClient transport)
        {
            this.transport = transport;
        }

        public override async UniTask StartConnection()
        {
            ValidateIsStopped();

            Status = LocalConnectionStatus.Starting;

            try
            {
                RegisterTransportEvents(transport);

                await transport.StartConnection();
            }
            catch (Exception e)
            {
                StopConnection();

                throw new Exception($"Exception thrown while starting {GetType().Name}", e);
            }

            Status = LocalConnectionStatus.Started;

            NotifyPacketHandlers_NetworkStarted();
        }

        public override void StopConnection()
        {
            transport.StopConnection();

            UnregisterTransportEvents(transport);

            Status = LocalConnectionStatus.Stopped;

            NotifyPacketHandlers_NetworkStopped();
        }

        protected override bool AreTransportsAllStarted()
        {
            return transport.Status == LocalConnectionStatus.Started;
        }

        protected override void OnConnectionRemoved(NetworkConnection connection)
        {
            base.OnConnectionRemoved(connection);

            // Server has disconnected
            StopConnection();
        }
    }
}
