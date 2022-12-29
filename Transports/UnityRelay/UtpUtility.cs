using System.Collections.Generic;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;

namespace Exanite.Networking.Transports.UnityRelay
{
    /// <summary>
    ///     Helper methods from Unity Relay docs:
    ///     https://docs.unity.com/relay/relay-and-utp.html
    /// </summary>
    public static class UtpUtility
    {
        public static RelayServerData CreateHostRelayData(Allocation allocation, string connectionType = "dtls")
        {
            // Select endpoint based on desired connectionType
            var endpoint = GetEndpointForConnectionType(allocation.ServerEndpoints, connectionType);
            if (endpoint == null)
            {
                throw new NetworkException($"endpoint for connectionType {connectionType} not found");
            }

            // Prepare the server endpoint using the Relay server IP and port
            var serverEndpoint = NetworkEndPoint.Parse(endpoint.Host, (ushort)endpoint.Port);

            // UTP uses pointers instead of managed arrays for performance reasons, so we use these helper functions to convert them
            var allocationIdBytes = ConvertFromAllocationIdBytes(allocation.AllocationIdBytes);
            var connectionData = ConvertConnectionData(allocation.ConnectionData);
            var key = ConvertFromHmac(allocation.Key);

            // The host passes its connectionData twice into this function
            var relayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationIdBytes, ref connectionData,
                ref connectionData, ref key, connectionType == "dtls");

            return relayServerData;
        }

        public static RelayServerData CreatePlayerRelayData(JoinAllocation allocation, string connectionType = "dtls")
        {
            // Select endpoint based on desired connectionType
            var endpoint = GetEndpointForConnectionType(allocation.ServerEndpoints, connectionType);
            if (endpoint == null)
            {
                throw new NetworkException($"endpoint for connectionType {connectionType} not found");
            }

            // Prepare the server endpoint using the Relay server IP and port
            var serverEndpoint = NetworkEndPoint.Parse(endpoint.Host, (ushort)endpoint.Port);

            // UTP uses pointers instead of managed arrays for performance reasons, so we use these helper functions to convert them
            var allocationIdBytes = ConvertFromAllocationIdBytes(allocation.AllocationIdBytes);
            var connectionData = ConvertConnectionData(allocation.ConnectionData);
            var hostConnectionData = ConvertConnectionData(allocation.HostConnectionData);
            var key = ConvertFromHmac(allocation.Key);

            // A player joining the host passes its own connectionData as well as the host's
            var relayServerData = new RelayServerData(ref serverEndpoint, 0, ref allocationIdBytes, ref connectionData,
                ref hostConnectionData, ref key, connectionType == "dtls");

            return relayServerData;
        }

        private static RelayAllocationId ConvertFromAllocationIdBytes(byte[] allocationIdBytes)
        {
            unsafe
            {
                fixed (byte* ptr = allocationIdBytes)
                {
                    return RelayAllocationId.FromBytePointer(ptr, allocationIdBytes.Length);
                }
            }
        }

        private static RelayConnectionData ConvertConnectionData(byte[] connectionData)
        {
            unsafe
            {
                fixed (byte* ptr = connectionData)
                {
                    return RelayConnectionData.FromBytePointer(ptr, RelayConnectionData.k_Length);
                }
            }
        }

        private static RelayHMACKey ConvertFromHmac(byte[] hmac)
        {
            unsafe
            {
                fixed (byte* ptr = hmac)
                {
                    return RelayHMACKey.FromBytePointer(ptr, RelayHMACKey.k_Length);
                }
            }
        }

        private static RelayServerEndpoint GetEndpointForConnectionType(List<RelayServerEndpoint> endpoints, string connectionType)
        {
            foreach (var endpoint in endpoints)
            {
                if (endpoint.ConnectionType == connectionType)
                {
                    return endpoint;
                }
            }

            return null;
        }
    }
}
