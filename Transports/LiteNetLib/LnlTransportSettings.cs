using UnityEngine;

namespace Exanite.Networking.Transports.LiteNetLib
{
    public class LnlTransportSettings : MonoBehaviour
    {
        [SerializeField] protected string connectionKey = Constants.DefaultConnectionKey;
        [SerializeField] private string remoteAddress = Constants.DefaultRemoteAddress;
        [SerializeField] private ushort port = Constants.DefaultPort;

        public string ConnectionKey
        {
            get => connectionKey;
            set => connectionKey = value;
        }

        public string RemoteAddress
        {
            get => remoteAddress;
            set => remoteAddress = value;
        }

        public ushort Port
        {
            get => port;
            set => port = value;
        }
    }
}
