using System.Collections.Generic;
using LiteNetLib.Utils;

namespace Exanite.Networking
{
    public interface IPacketHandlerManager : INetwork
    {
        public IReadOnlyDictionary<int, IPacketHandler> PacketHandlers { get; }

        public void RegisterPacketHandler(IPacketHandler handler);
        public void UnregisterPacketHandler(IPacketHandler handler);

        public void SendAsPacketHandler(IPacketHandler handler, NetworkConnection connection, NetDataWriter writer, SendType sendType);
    }
}
