using System;
using Cysharp.Threading.Tasks;
using LiteNetLib.Utils;
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

        public void Tick()
        {
            Driver.ScheduleUpdate().Complete();
        }

        public RemoteConnectionStatus GetConnectionStatus(NetworkConnection networkConnection)
        {
            throw new NotImplementedException();
        }

        public void SendData(ITransport connectionId, NetDataWriter writer, SendType sendType)
        {
            throw new NotImplementedException();
        }
    }

    public class UtpTransportClient : UtpTransport, ITransportClient
    {
        public async UniTask StartConnection()
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

        public void StopConnection()
        {
            Driver.Dispose();
        }

    }

    public class UtpTransportServer : UtpTransport, ITransportServer
    {
        public async UniTask StartConnection()
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

        public void StopConnection()
        {
            Driver.Dispose();
        }
    }
}
