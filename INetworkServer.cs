using System.Collections.Generic;
using Exanite.Networking.Transports;

namespace Exanite.Networking
{
    public interface INetworkServer : INetwork
    {
        public IReadOnlyList<ITransportServer> Transports { get; }
    }
}
