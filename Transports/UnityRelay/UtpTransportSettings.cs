#if UNITY_TRANSPORT
using UnityEngine;

namespace Exanite.Networking.Transports.UnityRelay
{
    public class UtpTransportSettings : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int maxConnections = 2;
        [SerializeField] private string joinCode = string.Empty;
        [SerializeField] private bool autoSignInToUnityServices = true;

        public int MaxConnections
        {
            get => maxConnections;
            set => maxConnections = value;
        }

        public string JoinCode
        {
            get => joinCode;
            set => joinCode = value?.ToUpper();
        }

        public bool AutoSignInToUnityServices
        {
            get => autoSignInToUnityServices;
            set => autoSignInToUnityServices = value;
        }
    }
}
#endif
