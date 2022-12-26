using System;
using Cysharp.Threading.Tasks;

namespace Exanite.Networking.Transports
{
    public interface ITransport
    {
        public LocalConnectionStatus Status { get; }
        public virtual bool IsReady => Status == LocalConnectionStatus.Started;

        public event ReceivedDataEvent ReceivedData;

        public void Tick();

        public UniTask StartConnection();
        public void StopConnection();

        RemoteConnectionStatus GetConnectionStatus(NetworkConnection networkConnection);

        void SendData(ITransport connectionId, ArraySegment<byte> data, SendType sendType);
    }

    public interface ITransportServer : ITransport {}

    public interface ITransportClient : ITransport {}
}
