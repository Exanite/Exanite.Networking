using System;
using Cysharp.Threading.Tasks;
using Exanite.Core.Events;

namespace Exanite.Networking.Transports
{
    public interface ITransport : IDisposable
    {
        public INetwork Network { get; set; }
        public LocalConnectionStatus Status { get; }
        public bool IsReady => Status == LocalConnectionStatus.Started;

        public event EventHandler<ITransport, TransportDataReceivedEventArgs> DataReceived;
        public event EventHandler<ITransport, TransportConnectionStatusEventArgs> ConnectionStatus;

        public void Tick();

        public UniTask StartConnection();
        public void StopConnection();

        public RemoteConnectionStatus GetConnectionStatus(int connectionId);

        /// <summary>
        /// Returns the MTU in bytes for a connection and send type.
        /// </summary>
        /// <remarks>
        /// Behavior is undefined for invalid connections.
        /// </remarks>
        public int GetMtu(int connectionId, SendType sendType);
        public void DisconnectConnection(int connectionId);

        public void SendData(int connectionId, ArraySegment<byte> data, SendType sendType);

        public void SetNetwork(INetwork network)
        {
            if (network == null)
            {
                Network = null;

                return;
            }

            if (Network != null)
            {
                throw new NetworkException("Transport is already being used in another network");
            }

            Network = network;
        }
    }
}
