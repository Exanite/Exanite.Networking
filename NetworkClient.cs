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
        public NetworkConnection ServerConnection => Connections.Values.FirstOrDefault();

        protected override void Awake()
        {
            base.Awake();

            ConnectionStopped += OnConnectionStopped;
        }

        protected override void OnDestroy()
        {
            ConnectionStopped -= OnConnectionStopped;

            base.OnDestroy();
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

                throw new NetworkException($"Exception thrown while starting {GetType().Name}", e);
            }

            Status = LocalConnectionStatus.Started;
        }

        public override void StopConnection()
        {
            transport.StopConnection();

            UnregisterTransportEvents(transport);

            Status = LocalConnectionStatus.Stopped;
        }

        protected override bool AreTransportsAllStarted()
        {
            return transport.Status == LocalConnectionStatus.Started;
        }

        protected override void OnTickTransports()
        {
            base.OnTickTransports();

            transport.Tick();
        }

        private void OnConnectionStopped(INetwork network, NetworkConnection connection)
        {
            // Disconnected from server
            StopConnection();
        }
    }
}
