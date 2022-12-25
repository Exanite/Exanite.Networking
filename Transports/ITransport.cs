using LiteNetLib.Utils;

namespace Exanite.Networking.Transports
{
    public interface ITransport
    {
        public void Tick();

        RemoteConnectionStatus GetConnectionStatus(NetworkConnection networkConnection);

        void SendData(ITransport connectionId, NetDataWriter writer, SendType sendType);
    }

    public interface ITransportServer: ITransport
    {
    }

    public interface ITransportClient: ITransport
    {
    }
}
