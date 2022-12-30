using System;
using Cysharp.Threading.Tasks;
using Exanite.Core.Events;

namespace Exanite.Networking.Transports
{
    public interface ITransport
    {
        public LocalConnectionStatus Status { get; }
        public virtual bool IsReady => Status == LocalConnectionStatus.Started;

        public event EventHandler<ITransport, TransportReceivedDataEventArgs> ReceivedData;
        public event EventHandler<ITransport, TransportConnectionStatusEventArgs> ConnectionStatus;

        public void Tick();

        public UniTask StartConnection();
        public void StopConnection();

        public RemoteConnectionStatus GetConnectionStatus(int connectionId);
        public void DisconnectConnection(int connectionId);

        public void SendData(int connectionId, ArraySegment<byte> data, SendType sendType);
    }

    public interface ITransportServer : ITransport {}

    public interface ITransportClient : ITransport {}
}
