#if UNITY_TRANSPORT
using Cysharp.Threading.Tasks;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using UnityConnectionStatus = Unity.Networking.Transport.NetworkConnection.State;

namespace Exanite.Networking.Transports.UnityRelay
{
    public class UtpTransportClient : UtpTransport, ITransportClient
    {
        public UtpTransportClient(UtpTransportSettings settings, IRelayService relayService, IAuthenticationService authenticationService) : base(settings, relayService, authenticationService) {}

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

            // Notice that Connect is a synchronous method.
            // The server connection begins in the Connecting state and we must wait until the connection succeeds or fails.
            var serverConnection = Driver.Connect(relayData.Endpoint);
            BeginTrackingConnection(serverConnection);

            Status = LocalConnectionStatus.Started;

            // Wait until connected or failed
            await UniTask.WaitWhile(() => Driver.GetConnectionState(serverConnection) == UnityConnectionStatus.Connecting);
            if (Driver.GetConnectionState(serverConnection) != UnityConnectionStatus.Connected)
            {
                Status = LocalConnectionStatus.Stopped;

                throw new NetworkException("Failed to connect.");
            }
        }
    }
}
#endif
