using Exanite.Networking.Transports;

namespace Exanite.Networking
{
    public class NetworkConnection
    {
        public int Id { get; internal set; }

        public ITransport Transport { get; internal set; }
    }
}
