using System;
using Cysharp.Threading.Tasks;
using UniDi;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using UnityEngine;

namespace Exanite.Networking.Transports.UnityRelay
{
    public abstract class UtpTransport : MonoBehaviour, ITransport
    {
        protected NetworkDriver Driver;

        [Inject] protected IRelayService RelayService;

        public LocalConnectionStatus Status { get; protected set; }

        public event TransportReceivedDataEvent ReceivedData;
        public event TransportConnectionStartedEvent ConnectionStarted;
        public event TransportConnectionStartedEvent ConnectionStopped;

        public void Tick()
        {
            Driver.ScheduleUpdate().Complete();
        }

        public abstract UniTask StartConnection();

        public void StopConnection()
        {
            Driver.Dispose();
        }

        public RemoteConnectionStatus GetConnectionStatus(int connectionId)
        {
            throw new NotImplementedException();
        }

        public void SendData(int connectionId, ArraySegment<byte> data, SendType sendType)
        {
            throw new NotImplementedException();
        }
    }

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

    public class UtpTransportServer : UtpTransport, ITransportServer
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

            Debug.Log("Yee connected!");
        }
    }
}
