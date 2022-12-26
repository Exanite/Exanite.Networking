using System;

namespace Exanite.Networking.Transports
{
    public delegate void TransportReceivedDataEvent(ITransport transport, int transportConnectionId, ArraySegment<byte> data, SendType sendType);
}
