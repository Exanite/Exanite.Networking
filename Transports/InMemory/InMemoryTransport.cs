using System;
using Cysharp.Threading.Tasks;
using Exanite.Core.Events;
using UnityEngine;

namespace Exanite.Networking.Transports.InMemory
{
    public class InMemoryTransport : MonoBehaviour, ITransport
    {
        public LocalConnectionStatus Status { get; }
        public event EventHandler<ITransport, TransportDataReceivedEventArgs> DataReceived;
        public event EventHandler<ITransport, TransportConnectionStatusEventArgs> ConnectionStatus;

        public void Tick()
        {
            throw new NotImplementedException();
        }

        public UniTask StartConnection()
        {
            throw new NotImplementedException();
        }

        public void StopConnection()
        {
            throw new NotImplementedException();
        }

        public RemoteConnectionStatus GetConnectionStatus(int connectionId)
        {
            throw new NotImplementedException();
        }

        public void DisconnectConnection(int connectionId)
        {
            throw new NotImplementedException();
        }

        public void SendData(int connectionId, ArraySegment<byte> data, SendType sendType)
        {
            throw new NotImplementedException();
        }
    }

    public class InMemoryTransportClient : InMemoryTransport, ITransportClient {}

    public class InMemoryTransportServer : InMemoryTransport, ITransportServer {}

    public class InMemoryTransportSettings : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int virtualAddress = 0;

        public int VirtualAddress
        {
            get => virtualAddress;
            set => virtualAddress = value;
        }
    }
}
