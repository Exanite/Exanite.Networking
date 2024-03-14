using System;
using UnityEngine;

namespace Exanite.Networking.Transports.InMemory
{
    [Serializable]
    public class InMemoryTransportSettings
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
