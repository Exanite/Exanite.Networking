using System;

namespace Exanite.Networking.Transports
{
    public readonly struct TransportDataReceivedEventArgs
    {
        public TransportDataReceivedEventArgs(int connectionId, ArraySegment<byte> data, SendType sendType)
        {
            ConnectionId = connectionId;
            Data = data;
            SendType = sendType;
        }

        public int ConnectionId { get; }

        public ArraySegment<byte> Data { get; }
        public SendType SendType { get; }
    }
}
