using Cysharp.Threading.Tasks;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;

namespace Exanite.Networking.Transports.UnityRelay
{
    public class UtpTransportServer : UtpTransport, ITransportServer
    {
        public override async UniTask StartConnection()
        {
            Status = LocalConnectionStatus.Starting;

            await SignInIfNeeded();

            var allocation = await RelayService.CreateAllocationAsync(2);
            var relayData = UtpUtility.CreateHostRelayData(allocation);

            var networkSettings = new NetworkSettings();
            networkSettings.WithRelayParameters(ref relayData);

            await CreateAndBindNetworkDriver(networkSettings);
            CreateNetworkPipelines();

            if (Driver.Listen() != 0)
            {
                throw new NetworkException("Failed to start listening to connections");
            }

            await UpdateJoinCode(allocation);

            Status = LocalConnectionStatus.Started;
        }

        private async UniTask UpdateJoinCode(Allocation allocation)
        {
            var joinCode = await RelayService.GetJoinCodeAsync(allocation.AllocationId);
            Settings.JoinCode = joinCode;
        }
    }
}
