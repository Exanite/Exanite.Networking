using UnityEngine;

namespace Exanite.Networking.Transports.InMemory
{
    public class InMemoryTransportSettings : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int virtualPort = 0;

        public int VirtualPort
        {
            get => virtualPort;
            set => virtualPort = value;
        }
    }
}
