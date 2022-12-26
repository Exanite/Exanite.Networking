using Cysharp.Threading.Tasks;
using LiteNetLib;

namespace Exanite.Networking.Transports.LiteNetLib
{
    public class LnlTransportServer : LnlTransport, ITransportServer
    {
        public override UniTask StartConnection()
        {
            netManager.Start(Port);

            return UniTask.CompletedTask;
        }

        public void DisconnectPeer(NetPeer peer)
        {
            netManager.DisconnectPeer(peer);
        }

        protected override void OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey(ConnectionKey);
        }
    }
}
