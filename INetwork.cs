using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LiteNetLib.Utils;

namespace Exanite.Networking
{
    public interface INetwork
    {
        public bool IsServer { get; }
        public bool IsClient => !IsServer;

        public LocalConnectionStatus Status { get; }
        public bool IsReady { get; }

        public IReadOnlyDictionary<int, IPacketHandler> PacketHandlers { get; }
        public IReadOnlyDictionary<int, NetworkConnection> Connections { get; }

        public event ConnectionStartedEvent ConnectionStarted;
        public event ConnectionStoppedEvent ConnectionStopped;

        public void Tick();

        public UniTask StartConnection();
        public void StopConnection();

        public void RegisterPacketHandler(IPacketHandler handler);
        public void UnregisterPacketHandler(IPacketHandler handler);

        public void SendAsPacketHandler(IPacketHandler handler, NetworkConnection connection, NetDataWriter writer, SendType sendType);
    }
}
