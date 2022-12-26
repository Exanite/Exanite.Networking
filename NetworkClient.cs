using System.Linq;
using Cysharp.Threading.Tasks;
using Exanite.Networking.Transports;
using Sirenix.Serialization;

namespace Exanite.Networking
{
    public class NetworkClient : Network
    {
        [OdinSerialize] private ITransportClient transport;

        public ITransportClient Transport => transport;
        public NetworkConnection Server => connectionTracker.Connections.Values.FirstOrDefault();

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
            catch
            {
                StopConnection();
            }

            Status = LocalConnectionStatus.Started;
        }

        public override void StopConnection()
        {
            transport.StopConnection();

            UnregisterTransportEvents(transport);

            Status = LocalConnectionStatus.Stopped;
        }
    }
}
