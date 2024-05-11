#if UNITY_TRANSPORT
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Exanite.Core.Collections;
using Exanite.Core.Events;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Error;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityNetworkConnection = Unity.Networking.Transport.NetworkConnection;
using UnityConnectionStatus = Unity.Networking.Transport.NetworkConnection.State;

namespace Exanite.Networking.Transports.UnityRelay
{
    public class UtpTransport : ITransport
    {
        private NetworkDriver driver;
        private NetworkPipeline reliablePipeline;
        private NetworkPipeline unreliablePipeline;

        private int nextConnectionId;
        private TwoWayDictionary<int, UnityNetworkConnection> connections = new();
        private List<int> connectionIdsToRemove = new();

        private Queue<TransportConnectionStatusEventArgs> connectionEventQueue = new();

        private readonly IRelayService relayService;
        private readonly IAuthenticationService authenticationService;

        public UtpTransportSettings Settings { get; }

        public INetwork Network { get; set; }
        public LocalConnectionStatus Status { get; private set; }

        public event EventHandler<ITransport, TransportDataReceivedEventArgs> DataReceived;
        public event EventHandler<ITransport, TransportConnectionStatusEventArgs> ConnectionStatus;

        public UtpTransport(UtpTransportSettings settings, IRelayService relayService, IAuthenticationService authenticationService)
        {
            Settings = settings;
            this.relayService = relayService;
            this.authenticationService = authenticationService;
        }

        public void Dispose()
        {
            StopConnection(false);
        }

        public void Tick()
        {
            // Based off of Unity's Simple Relay Sample (using UTP) package
            if (Status != LocalConnectionStatus.Started)
            {
                return;
            }

            driver.ScheduleUpdate().Complete();

            RemoveDisconnectedConnections();

            UnityNetworkConnection incomingConnection;
            while ((incomingConnection = driver.Accept()) != default)
            {
                // Accepted connections are immediately ready.
                BeginTrackingConnection(incomingConnection);
                OnConnectionReady(incomingConnection);
            }

            foreach (var (_, connection) in connections)
            {
                NetworkEvent.Type networkEvent;
                while ((networkEvent = driver.PopEventForConnection(connection, out var stream, out var pipeline)) != NetworkEvent.Type.Empty)
                {
                    switch (networkEvent)
                    {
                        case NetworkEvent.Type.Connect:
                        {
                            // While this looks like a general connect event,
                            // this is only received on the client when the connection to the server is fully established.
                            // All other connections are handled above.
                            OnConnectionReady(connection);

                            break;
                        }
                        case NetworkEvent.Type.Data:
                        {
                            OnNetworkReceive(stream, connection, pipeline);

                            break;
                        }
                        case NetworkEvent.Type.Disconnect:
                        {
                            if (connections.Inverse.TryGetValue(connection, out var connectionId))
                            {
                                connectionIdsToRemove.Add(connectionId);
                            }

                            break;
                        }
                    }
                }
            }

            PushEvents();
        }

        private void RemoveDisconnectedConnections()
        {
            foreach (var (connectionId, connection) in connections)
            {
                if (!connection.IsCreated)
                {
                    connectionIdsToRemove.Add(connectionId);
                }
            }

            foreach (var connectionId in connectionIdsToRemove)
            {
                OnConnectionStopped(connectionId);
            }

            connectionIdsToRemove.Clear();
        }

        public async UniTask StartConnection()
        {
            if (Network.IsServer)
            {
                Status = LocalConnectionStatus.Starting;

                await SignInIfNeeded();

                var allocation = await relayService.CreateAllocationAsync(Settings.MaxConnections);
                var relayData = UtpUtility.CreateHostRelayData(allocation);

                var networkSettings = new NetworkSettings();
                networkSettings.WithRelayParameters(ref relayData);

                await CreateAndBindNetworkDriver(networkSettings);
                CreateNetworkPipelines();

                if (driver.Listen() != 0)
                {
                    throw new NetworkException("Failed to start listening to connections");
                }

                await UpdateJoinCode(allocation);

                Status = LocalConnectionStatus.Started;
            }

            if (Network.IsClient)
            {
                Status = LocalConnectionStatus.Starting;

                await SignInIfNeeded();

                var allocation = await relayService.JoinAllocationAsync(Settings.JoinCode);
                var relayData = UtpUtility.CreatePlayerRelayData(allocation);

                var networkSettings = new NetworkSettings();
                networkSettings.WithRelayParameters(ref relayData);

                await CreateAndBindNetworkDriver(networkSettings);
                CreateNetworkPipelines();

                // Notice that Connect is a synchronous method.
                // The server connection begins in the Connecting state and we must wait until the connection succeeds or fails.
                var serverConnection = driver.Connect(relayData.Endpoint);
                BeginTrackingConnection(serverConnection);

                Status = LocalConnectionStatus.Started;

                // Wait until connected or failed
                await UniTask.WaitWhile(() => driver.GetConnectionState(serverConnection) == UnityConnectionStatus.Connecting);
                if (driver.GetConnectionState(serverConnection) != UnityConnectionStatus.Connected)
                {
                    Status = LocalConnectionStatus.Stopped;

                    throw new NetworkException("Failed to connect.");
                }
            }
        }

        private async UniTask UpdateJoinCode(Allocation allocation)
        {
            var joinCode = await relayService.GetJoinCodeAsync(allocation.AllocationId);
            Settings.JoinCode = joinCode;
        }

        public void StopConnection()
        {
            StopConnection(true);
        }

        private void StopConnection(bool handleEvents)
        {
            try
            {
                if (Status != LocalConnectionStatus.Stopped)
                {
                    foreach (var (connectionId, connection) in connections)
                    {
                        connection.Disconnect(driver);
                        connectionIdsToRemove.Add(connectionId);
                    }

                    RemoveDisconnectedConnections();

                    if (driver.IsCreated)
                    {
                        driver.ScheduleUpdate().Complete();
                    }

                    if (handleEvents)
                    {
                        PushEvents();
                    }
                }
            }
            finally
            {
                connections.Clear();
                connectionIdsToRemove.Clear();

                driver.Dispose();

                Status = LocalConnectionStatus.Stopped;
            }
        }

        public RemoteConnectionStatus GetConnectionStatus(int connectionId)
        {
            return connections.ContainsKey(connectionId) ? RemoteConnectionStatus.Started : RemoteConnectionStatus.Stopped;
        }

        public int GetMtu(int connectionId, SendType sendType)
        {
            if (sendType == SendType.Unreliable)
            {
                return NetworkParameterConstants.MTU - driver.MaxHeaderSize(unreliablePipeline);
            }

            // Todo Figure out how to calculate MTU for reliable pipeline
            return int.MaxValue;
        }

        public void DisconnectConnection(int connectionId)
        {
            if (connections.TryGetValue(connectionId, out var connection))
            {
                connection.Disconnect(driver);
                connectionIdsToRemove.Add(connectionId);
            }
        }

        public void SendData(int connectionId, ArraySegment<byte> data, SendType sendType)
        {
            if (!connections.TryGetValue(connectionId, out var connection))
            {
                throw new NetworkException("Attempted to send data to invalid connection.");
            }

            // Based off of Unity's Simple Relay Sample (using UTP) package
            var pipeline = sendType == SendType.Reliable ? reliablePipeline : unreliablePipeline;

            using var buffer = new NativeArray<byte>(data.Count, Allocator.Temp);
            NativeArray<byte>.Copy(data.Array, data.Offset, buffer, 0, data.Count);

            var writeStatus = driver.BeginSend(pipeline, connection, out var writer);
            if (writeStatus != (int)StatusCode.Success)
            {
                throw new NetworkException($"Failed to send data: {(StatusCode)writeStatus}");
            }

            writer.WriteBytes(buffer);
            driver.EndSend(writer);
        }

        private async UniTask SignInIfNeeded()
        {
            if (Settings.AutoSignInToUnityServices && !authenticationService.IsSignedIn)
            {
                await authenticationService.SignInAnonymouslyAsync();
            }
        }

        private async UniTask CreateAndBindNetworkDriver(NetworkSettings networkSettings)
        {
            driver = NetworkDriver.Create(networkSettings);

            if (driver.Bind(NetworkEndpoint.AnyIpv4) != 0)
            {
                throw new NetworkException("Failed to bind to local address");
            }

            while (!driver.Bound)
            {
                driver.ScheduleUpdate().Complete();

                await UniTask.Yield();
            }
        }

        private void CreateNetworkPipelines()
        {
            reliablePipeline = driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));
            unreliablePipeline = driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));
        }

        private void PushEvents()
        {
            while (connectionEventQueue.TryDequeue(out var e))
            {
                ConnectionStatus?.Invoke(this, e);
            }
        }

        /// <summary>
        /// Begin tracking the connection and polling events for the connection.
        /// <para/>
        /// Must be called before <see cref="OnConnectionReady"/>>.
        /// </summary>
        private void BeginTrackingConnection(UnityNetworkConnection connection)
        {
            connections.Add(nextConnectionId++, connection);
        }

        /// <summary>
        /// Notify event listeners that the connection is ready.
        /// <para/>
        /// Must be called after <see cref="BeginTrackingConnection"/>.
        /// </summary>
        private void OnConnectionReady(UnityNetworkConnection connection)
        {
            if (connections.Inverse.TryGetValue(connection, out var connectionId))
            {
                connectionEventQueue.Enqueue(new TransportConnectionStatusEventArgs(connectionId, RemoteConnectionStatus.Started));
            }
        }

        private void OnConnectionStopped(int connectionId)
        {
            if (connections.Remove(connectionId))
            {
                connectionEventQueue.Enqueue(new TransportConnectionStatusEventArgs(connectionId, RemoteConnectionStatus.Stopped));
            }
        }

        private void OnNetworkReceive(DataStreamReader stream, UnityNetworkConnection connection, NetworkPipeline pipeline)
        {
            // Based off of Unity's Simple Relay Sample (using UTP) package
            using var buffer = new NativeArray<byte>(stream.Length, Allocator.Temp);
            stream.ReadBytes(buffer);

            var data = new ArraySegment<byte>(buffer.ToArray());
            var sendType = pipeline == reliablePipeline ? SendType.Reliable : SendType.Unreliable;

            if (connections.Inverse.TryGetValue(connection, out var connectionId))
            {
                DataReceived?.Invoke(this, new TransportDataReceivedEventArgs(connectionId, data, sendType));
            }
        }
    }
}
#endif
