using System;
using System.Net;
using Cysharp.Threading.Tasks;
using Exanite.Core.Events;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Exanite.Networking.Client
{
    public class UnityClient : UnityNetwork
    {
        private DisconnectInfo previousDisconnectInfo;

        public NetPeer Server { get; private set; }

        public ConnectionStatus Status { get; private set; }
        public override bool IsReady => Status == ConnectionStatus.Started;

        public event EventHandler<UnityClient, ConnectedEventArgs> Connected;
        public event EventHandler<UnityClient, DisconnectedEventArgs> Disconnected;

        private void OnDestroy()
        {
            Disconnect(false);
        }

        public async UniTask<ConnectResult> ConnectAsync(IPEndPoint endPoint)
        {
            switch (Status)
            {
                case ConnectionStatus.Starting: throw new InvalidOperationException("Client is already connecting.");
                case ConnectionStatus.Started: throw new InvalidOperationException("Client is already connected.");
            }

            Status = ConnectionStatus.Starting;

            netManager.Start();
            netManager.Connect(endPoint, ConnectionKey);

            await UniTask.WaitUntil(() => Status != ConnectionStatus.Starting);

            return new ConnectResult(Status == ConnectionStatus.Started, previousDisconnectInfo.Reason.ToString());
        }

        public void Disconnect()
        {
            Disconnect(true);
        }

        protected void Disconnect(bool pollEvents)
        {
            netManager.DisconnectAll();

            if (pollEvents)
            {
                netManager.PollEvents();
            }

            netManager.Stop();

            Status = ConnectionStatus.Stopped;
        }

        public void SendAsPacketHandlerToServer(IPacketHandler handler, NetDataWriter writer, DeliveryMethod deliveryMethod)
        {
            ValidateIsReadyToSend();

            WritePacketHandlerDataToCachedWriter(handler, writer);
            Server.Send(cachedWriter, deliveryMethod);
        }

        protected override void OnPeerConnected(NetPeer server)
        {
            base.OnPeerConnected(server);

            Connected?.Invoke(this, new ConnectedEventArgs(server));

            Status = ConnectionStatus.Started;

            Server = server;
        }

        protected override void OnPeerDisconnected(NetPeer server, DisconnectInfo disconnectInfo)
        {
            base.OnPeerDisconnected(server, disconnectInfo);

            if (Status == ConnectionStatus.Started)
            {
                Disconnected?.Invoke(this, new DisconnectedEventArgs(server, disconnectInfo));
            }

            Status = ConnectionStatus.Stopped;

            Server = null;
            previousDisconnectInfo = disconnectInfo;
        }

        protected override void OnConnectionRequest(ConnectionRequest request)
        {
            request.Reject();
        }
    }
}
