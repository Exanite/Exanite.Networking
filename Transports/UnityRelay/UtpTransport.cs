using System;
using Cysharp.Threading.Tasks;
using UniDi;
using Unity.Networking.Transport;
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

        public void DisconnectConnection(int connectionId)
        {
            throw new NotImplementedException();
        }

        public void SendData(int connectionId, ArraySegment<byte> data, SendType sendType)
        {
            throw new NotImplementedException();
        }
    }
}
