using System;
using Cysharp.Threading.Tasks;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using UnityEngine;

namespace Exanite.Networking.Transports.UnityRelay
{
    public class UnityRelayTransport : ITransport<UnityRelayTransportServer, UnityRelayTransportClient>
    {
        public UnityRelayTransport()
        {
            Server = new UnityRelayTransportServer();
            Client = new UnityRelayTransportClient();
        }

        public UnityRelayTransportServer Server { get; }
        public UnityRelayTransportClient Client { get; }
    }

    public class UnityRelayTransportClient : ITransportClient
    {
        private IRelayService relayService;

        private NetworkDriver clientDriver;

        public void Tick()
        {

        }

        public async UniTask StartConnection()
        {
            var allocation = await relayService.CreateAllocationAsync(2);

            var relayData = UnityRelayUtility.CreateHostRelayData(allocation);

            var networkSettings = new NetworkSettings();
            networkSettings.WithRelayParameters(ref relayData);

            clientDriver = NetworkDriver.Create(networkSettings);
            if (clientDriver.Bind(NetworkEndPoint.AnyIpv4) != 0)
            {
                throw new Exception("Failed to bind to local address");
            }

            await UniTask.WaitWhile(() => clientDriver.Bound);

            if (clientDriver.Listen() != 0)
            {
                throw new Exception("Failed to start listening to connections");
            }
        }

        public void StopConnection() {}
    }

    public class UnityRelayTransportServer : ITransportServer
    {
        private IRelayService relayService;

        private NetworkDriver serverDriver;

        public void Tick()
        {
            if (serverDriver.IsCreated)
            {
                serverDriver.ScheduleUpdate().Complete();
            }
        }

        public async UniTask StartConnection()
        {
            var allocation = await relayService.CreateAllocationAsync(2);

            var relayData = UnityRelayUtility.CreateHostRelayData(allocation);

            var networkSettings = new NetworkSettings();
            networkSettings.WithRelayParameters(ref relayData);

            serverDriver = NetworkDriver.Create(networkSettings);
            if (serverDriver.Bind(NetworkEndPoint.AnyIpv4) != 0)
            {
                throw new Exception("Failed to bind to local address");
            }

            await UniTask.WaitWhile(() => serverDriver.Bound);

            if (serverDriver.Listen() != 0)
            {
                throw new Exception("Failed to start listening to connections");
            }

            Debug.Log("Yee connected!");
        }

        public void StopConnection()
        {
            serverDriver.Dispose();
        }
    }
}
