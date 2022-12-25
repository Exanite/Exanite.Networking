using System;

namespace Exanite.Networking.Transports
{
    public interface ITransport
    {
        public void Tick();

        RemoteConnectionStatus GetConnectionStatus(NetworkConnection networkConnection);

        void SendData(ITransport connectionId, ArraySegment<byte> data, SendType sendType);
    }

    public interface ITransportServer : ITransport {}

    public interface ITransportClient : ITransport {}
}
