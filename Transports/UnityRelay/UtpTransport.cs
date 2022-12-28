using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UniDi;
using Unity.Networking.Transport;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using UnityEngine;
using UnityNetworkConnection = Unity.Networking.Transport.NetworkConnection;

namespace Exanite.Networking.Transports.UnityRelay
{
    public abstract class UtpTransport : MonoBehaviour, ITransport
    {
        [Header("Dependencies")]
        [Required] [SerializeField] private UtpTransportSettings settings;

        protected NetworkDriver Driver;
        protected NetworkPipeline ReliablePipeline;
        protected NetworkPipeline UnreliablePipeline;

        protected Dictionary<int, UnityNetworkConnection> connections;

        [Inject] protected IRelayService RelayService;
        [Inject] protected IAuthenticationService AuthenticationService;

        public UtpTransportSettings Settings => settings;

        public LocalConnectionStatus Status { get; protected set; }

        public event TransportReceivedDataEvent ReceivedData;
        public event TransportConnectionStartedEvent ConnectionStarted;
        public event TransportConnectionStartedEvent ConnectionStopped;

        private void OnDestroy()
        {
            Driver.Dispose();
        }

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
            throw new NotImplementedException();
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
            if (Settings.AutoSignInToUnityServices && !AuthenticationService.IsSignedIn)
            {
                await AuthenticationService.SignInAnonymouslyAsync();
            }
        }

        protected async UniTask CreateAndBindNetworkDriver(NetworkSettings networkSettings)
        {
            Driver = NetworkDriver.Create(networkSettings);
            if (Driver.Bind(NetworkEndPoint.AnyIpv4) != 0)
            {
                throw new Exception("Failed to bind to local address");
            }

            while (!Driver.Bound)
            {
                Driver.ScheduleUpdate().Complete();

                await UniTask.Yield();
            }
        }

        protected void CreateNetworkPipelines()
        {
            ReliablePipeline = Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            UnreliablePipeline = Driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));
        }

        protected virtual void OnConnectionStarted(UnityNetworkConnection connection)
        {
            connections.Add(connection.InternalId, connection);

            ConnectionStarted?.Invoke(this, connection.InternalId);
        }

        protected virtual void OnConnectionStopped(UnityNetworkConnection connection)
        {
            connections.Remove(connection.InternalId);

            ConnectionStopped?.Invoke(this, connection.InternalId);
        }
    }
}
