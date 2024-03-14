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
        private ITransport transport;

        public override bool IsServer => false;
        public ITransport Transport => transport;
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

                transport.SetNetwork(this);
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
            transport.SetNetwork(null);

            UnregisterTransportEvents(transport);

            Status = LocalConnectionStatus.Stopped;
        }

        public void SetTransport(ITransport transport)
        {
            if (Status != LocalConnectionStatus.Stopped)
            {
                throw new NetworkException($"Setting transports is only possible when the {GetType().Name} is stopped");
            }

            this.transport = transport;
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
