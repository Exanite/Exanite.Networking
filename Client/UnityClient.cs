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

        public LocalConnectionStatus Status { get; private set; }
        public override bool IsReady => Status == LocalConnectionStatus.Started;

        public event EventHandler<UnityClient, ClientConnectedEventArgs> Connected;
        public event EventHandler<UnityClient, ClientDisconnectedEventArgs> Disconnected;

        protected override void OnDestroy()
        {
            Disconnect(false);

            base.OnDestroy();
        }

        public async UniTask<ClientConnectResult> ConnectAsync(IPEndPoint endPoint)
        {
            switch (Status)
            {
                case LocalConnectionStatus.Starting: throw new InvalidOperationException("Client is already connecting.");
                case LocalConnectionStatus.Started: throw new InvalidOperationException("Client is already connected.");
            }

            Status = LocalConnectionStatus.Starting;

            netManager.Start();
            netManager.Connect(endPoint, ConnectionKey);

            await UniTask.WaitUntil(() => Status != LocalConnectionStatus.Starting);

            return new ClientConnectResult(Status == LocalConnectionStatus.Started, previousDisconnectInfo.Reason.ToString());
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

            Status = LocalConnectionStatus.Stopped;
        }

        public void SendAsPacketHandlerToServer(IPacketHandler handler, NetDataWriter writer, DeliveryMethod deliveryMethod)
        {
            ValidateIsReadyToSend();

            WritePacketHandlerDataToCachedWriter(handler, writer);
            Server.Send(cachedWriter, deliveryMethod);
        }

        protected override void OnPeerConnected(NetPeer server)
        {
            Connected?.Invoke(this, new ClientConnectedEventArgs(server));

            Status = LocalConnectionStatus.Started;

            Server = server;
        }

        protected override void OnPeerDisconnected(NetPeer server, DisconnectInfo disconnectInfo)
        {
            if (Status == LocalConnectionStatus.Started)
            {
                Disconnected?.Invoke(this, new ClientDisconnectedEventArgs(server, disconnectInfo));
            }

            Status = LocalConnectionStatus.Stopped;

            Server = null;
            previousDisconnectInfo = disconnectInfo;
        }

        protected override void OnConnectionRequest(ConnectionRequest request)
        {
            request.Reject();
        }
    }
}
