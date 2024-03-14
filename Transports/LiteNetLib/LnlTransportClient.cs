using Cysharp.Threading.Tasks;
using LiteNetLib;

namespace Exanite.Networking.Transports.LiteNetLib
{
    public class LnlTransportClient : LnlTransport, ITransportClient
    {
        public LnlTransportClient(LnlTransportSettings settings) : base(settings) {}

        public override async UniTask StartConnection()
        {
            Status = LocalConnectionStatus.Starting;

            netManager.Start();
            netManager.Connect(Settings.RemoteAddress, Settings.Port, Settings.ConnectionKey);

            await UniTask.WaitUntil(() => Status != LocalConnectionStatus.Starting);

            if (Status != LocalConnectionStatus.Started)
            {
                throw new NetworkException("Failed to connect.");
            }
        }

        protected override void OnPeerConnected(NetPeer peer)
        {
            Status = LocalConnectionStatus.Started;

            base.OnPeerConnected(peer);
        }

        protected override void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if (Status == LocalConnectionStatus.Started)
            {
                Status = LocalConnectionStatus.Stopped;

                base.OnPeerDisconnected(peer, disconnectInfo);
            }

            Status = LocalConnectionStatus.Stopped;
        }

        protected override void OnConnectionRequest(ConnectionRequest request)
        {
            request.Reject();
        }
    }
}
