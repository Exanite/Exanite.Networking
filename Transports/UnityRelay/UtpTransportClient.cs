using System;
using Cysharp.Threading.Tasks;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;

namespace Exanite.Networking.Transports.UnityRelay
{
    public class UtpTransportClient : UtpTransport, ITransportClient
    {
        public override async UniTask StartConnection()
        {
            var allocation = await RelayService.CreateAllocationAsync(2);

            var relayData = UtpUtility.CreateHostRelayData(allocation);

            var networkSettings = new NetworkSettings();
            networkSettings.WithRelayParameters(ref relayData);

            Driver = NetworkDriver.Create(networkSettings);
            if (Driver.Bind(NetworkEndPoint.AnyIpv4) != 0)
            {
                throw new Exception("Failed to bind to local address");
            }

            await UniTask.WaitWhile(() => Driver.Bound);

            if (Driver.Listen() != 0)
            {
                throw new Exception("Failed to start listening to connections");
            }
        }
    }
}
