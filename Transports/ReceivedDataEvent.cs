using System;

namespace Exanite.Networking.Transports
{
    public delegate void ReceivedDataEvent(ITransport transport, int transportConnectionId, ArraySegment<byte> data, SendType sendType);
}
