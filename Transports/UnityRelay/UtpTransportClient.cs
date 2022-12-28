using Cysharp.Threading.Tasks;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;

namespace Exanite.Networking.Transports.UnityRelay
{
    public class UtpTransportClient : UtpTransport, ITransportClient
    {
        public override async UniTask StartConnection()
        {
            Status = LocalConnectionStatus.Starting;

            await SignInIfNeeded();

            var allocation = await RelayService.JoinAllocationAsync(Settings.JoinCode);
            var relayData = UtpUtility.CreatePlayerRelayData(allocation);

            var networkSettings = new NetworkSettings();
            networkSettings.WithRelayParameters(ref relayData);

            await CreateAndBindNetworkDriver(networkSettings);
            CreateNetworkPipelines();

            var serverConnection = Driver.Connect(relayData.Endpoint);
            OnConnectionStarted(serverConnection);

            Status = LocalConnectionStatus.Started;
        }
    }
}
