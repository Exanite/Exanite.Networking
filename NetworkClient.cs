using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Exanite.Networking.Transports;
#if ODIN_INSPECTOR
using Sirenix.Serialization;
#endif

namespace Exanite.Networking
{
    public class NetworkClient : Network
    {
#if ODIN_INSPECTOR
        [OdinSerialize]
#endif
        private ITransportClient transport;

        public override bool IsServer => false;

        public ITransportClient Transport
        {
            get => transport;
            set => transport = value;
        }

        public NetworkConnection ServerConnection => Connections.FirstOrDefault();

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

        protected override bool AreAnyTransportsStopped()
        {
            return transport.Status == LocalConnectionStatus.Stopped;
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
