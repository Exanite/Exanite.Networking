using System;
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
            await SignInIfNeeded();

            var allocation = await RelayService.CreateAllocationAsync(2);
            var relayData = UtpUtility.CreateHostRelayData(allocation);

            var networkSettings = new NetworkSettings();
            networkSettings.WithRelayParameters(ref relayData);

            Driver = await CreateAndBindNetworkDriver(networkSettings);

            if (Driver.Listen() != 0)
            {
                throw new Exception("Failed to start listening to connections");
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