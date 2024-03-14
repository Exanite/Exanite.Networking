using Cysharp.Threading.Tasks;
using LiteNetLib;

namespace Exanite.Networking.Transports.LiteNetLib
{
    public class LnlTransportServer : LnlTransport, ITransportServer
    {
        public LnlTransportServer(LnlTransportSettings settings) : base(settings) {}

        public override UniTask StartConnection()
        {
            netManager.Start(Settings.Port);

            Status = LocalConnectionStatus.Started;

            return UniTask.CompletedTask;
        }

        protected override void OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey(Settings.ConnectionKey);
        }
    }
}
