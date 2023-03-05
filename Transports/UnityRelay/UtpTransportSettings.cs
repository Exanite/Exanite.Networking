using UnityEngine;

namespace Exanite.Networking.Transports.UnityRelay
{
    public class UtpTransportSettings : MonoBehaviour
    {
        [SerializeField] private string joinCode = string.Empty;
        [SerializeField] private bool autoSignInToUnityServices = true;

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
