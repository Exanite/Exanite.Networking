using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UniDi;
using Unity.Networking.Transport;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using UnityEngine;

namespace Exanite.Networking.Transports.UnityRelay
{
    public abstract class UtpTransport : MonoBehaviour, ITransport
    {
        [Header("Dependencies")]
        [Required] [SerializeField] private UtpTransportSettings settings;

        protected NetworkDriver Driver;

        [Inject] protected IRelayService RelayService;
        [Inject] protected IAuthenticationService AuthenticationService;

        public UtpTransportSettings Settings => settings;

        public LocalConnectionStatus Status { get; protected set; }

        public event TransportReceivedDataEvent ReceivedData;
        public event TransportConnectionStartedEvent ConnectionStarted;
        public event TransportConnectionStartedEvent ConnectionStopped;

        public void Tick()
        {
            if (Status == LocalConnectionStatus.Started)
            {
                Driver.ScheduleUpdate().Complete();
            }
        }

        public abstract UniTask StartConnection();

        public void StopConnection()
        {
            if (Driver.IsCreated)
            {
                Driver.Dispose();
            }
        }

        public RemoteConnectionStatus GetConnectionStatus(int connectionId)
        {
            throw new NotImplementedException();
        }

        public void DisconnectConnection(int connectionId)
        {
            throw new NotImplementedException();
        }

        public void SendData(int connectionId, ArraySegment<byte> data, SendType sendType)
        {
            throw new NotImplementedException();
        }

        protected async UniTask SignInIfNeeded()
        {
            if (Settings.AutoSignInToUnityServices)
            {
                await AuthenticationService.SignInAnonymouslyAsync();
            }
        }

        protected async UniTask<NetworkDriver> CreateAndBindNetworkDriver(NetworkSettings networkSettings)
        {
            var driver = NetworkDriver.Create(networkSettings);
            if (driver.Bind(NetworkEndPoint.AnyIpv4) != 0)
            {
                throw new Exception("Failed to bind to local address");
            }

            while (!driver.Bound)
            {
                Driver.ScheduleUpdate().Complete();

                await UniTask.Yield();
            }

            return driver;
        }
    }
}
