using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Exanite.Networking.Transports;

namespace Exanite.Networking
{
    public class NetworkClient : Network
    {
        private ITransport transport;

        public override bool IsServer => false;
        public ITransport Transport => transport;
        public NetworkConnection ServerConnection => Connections.FirstOrDefault();

        public NetworkClient()
        {
            ConnectionStopped += OnConnectionStopped;
        }

        public override void Dispose()
        {
            ConnectionStopped -= OnConnectionStopped;

            base.Dispose();
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
