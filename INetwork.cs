using System.Collections.Generic;

namespace Exanite.Networking
{
    public interface INetwork
    {
        public bool IsServer { get; }
        public bool IsClient { get; }

        public LocalConnectionStatus Status { get; }
        public bool IsReady { get; }

        public IEnumerable<NetworkConnection> Connections { get; }

        public event NetworkStartedEvent NetworkStarted;
        public event NetworkStoppedEvent NetworkStopped;

        public event ConnectionStartedEvent ConnectionStarted;
        public event ConnectionStoppedEvent ConnectionStopped;
    }
}
