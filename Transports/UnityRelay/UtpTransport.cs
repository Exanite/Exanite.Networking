#if UNITY_TRANSPORT
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
        protected NetworkDriver Driver;
        protected NetworkPipeline ReliablePipeline;
        protected NetworkPipeline UnreliablePipeline;

        protected Dictionary<int, UnityNetworkConnection> connections = new();
        protected List<int> connectionIdsToRemove = new();

        protected Queue<TransportConnectionStatusEventArgs> connectionEventQueue = new();

        protected readonly IRelayService RelayService;
        protected readonly IAuthenticationService AuthenticationService;

        public UtpTransportSettings Settings { get; }

        public INetwork Network { get; set; }
        public LocalConnectionStatus Status { get; protected set; }

        public event EventHandler<ITransport, TransportDataReceivedEventArgs> DataReceived;
        public event EventHandler<ITransport, TransportConnectionStatusEventArgs> ConnectionStatus;

        public UtpTransport(UtpTransportSettings settings, IRelayService relayService, IAuthenticationService authenticationService)
        {
            Settings = settings;
            RelayService = relayService;
            AuthenticationService = authenticationService;
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

            Driver.ScheduleUpdate().Complete();

            RemoveDisconnectedConnections();

            UnityNetworkConnection incomingConnection;
            while ((incomingConnection = Driver.Accept()) != default)
            {
                // Accepted connections are immediately ready.
                BeginTrackingConnection(incomingConnection);
                OnConnectionReady(incomingConnection);
            }

            foreach (var (_, connection) in connections)
            {
                NetworkEvent.Type networkEvent;
                while ((networkEvent = Driver.PopEventForConnection(connection, out var stream, out var pipeline)) != NetworkEvent.Type.Empty)
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
                            connectionIdsToRemove.Add(connection.InternalId);

                            break;
                        }
                    }
                }
            }

            PushEvents();
        }

        private void RemoveDisconnectedConnections()
        {
            foreach (var connection in connections.Values)
            {
                if (!connection.IsCreated)
                {
                    connectionIdsToRemove.Add(connection.InternalId);
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

                var allocation = await RelayService.CreateAllocationAsync(Settings.MaxConnections);
                var relayData = UtpUtility.CreateHostRelayData(allocation);

                var networkSettings = new NetworkSettings();
                networkSettings.WithRelayParameters(ref relayData);

                await CreateAndBindNetworkDriver(networkSettings);
                CreateNetworkPipelines();

                if (Driver.Listen() != 0)
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

                var allocation = await RelayService.JoinAllocationAsync(Settings.JoinCode);
                var relayData = UtpUtility.CreatePlayerRelayData(allocation);

                var networkSettings = new NetworkSettings();
                networkSettings.WithRelayParameters(ref relayData);

                await CreateAndBindNetworkDriver(networkSettings);
                CreateNetworkPipelines();

                // Notice that Connect is a synchronous method.
                // The server connection begins in the Connecting state and we must wait until the connection succeeds or fails.
                var serverConnection = Driver.Connect(relayData.Endpoint);
                BeginTrackingConnection(serverConnection);

                Status = LocalConnectionStatus.Started;

                // Wait until connected or failed
                await UniTask.WaitWhile(() => Driver.GetConnectionState(serverConnection) == UnityConnectionStatus.Connecting);
                if (Driver.GetConnectionState(serverConnection) != UnityConnectionStatus.Connected)
                {
                    Status = LocalConnectionStatus.Stopped;

                    throw new NetworkException("Failed to connect.");
                }
            }
        }

        private async UniTask UpdateJoinCode(Allocation allocation)
        {
            var joinCode = await RelayService.GetJoinCodeAsync(allocation.AllocationId);
            Settings.JoinCode = joinCode;
        }

        public void StopConnection()
        {
            StopConnection(true);
        }

        protected void StopConnection(bool handleEvents)
        {
            try
            {
                if (Status != LocalConnectionStatus.Stopped)
                {
                    foreach (var connection in connections.Values)
                    {
                        connection.Disconnect(Driver);
                        connectionIdsToRemove.Add(connection.InternalId);
                    }

                    RemoveDisconnectedConnections();

                    if (Driver.IsCreated)
                    {
                        Driver.ScheduleUpdate().Complete();
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

                Driver.Dispose();

                Status = LocalConnectionStatus.Stopped;
            }
        }

        public RemoteConnectionStatus GetConnectionStatus(int connectionId)
        {
            return connections.ContainsKey(connectionId) ? RemoteConnectionStatus.Started : RemoteConnectionStatus.Stopped;
        }

        public void DisconnectConnection(int connectionId)
        {
            if (connections.TryGetValue(connectionId, out var connection))
            {
                connection.Disconnect(Driver);
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
            var pipeline = sendType == SendType.Reliable ? ReliablePipeline : UnreliablePipeline;

            using var buffer = new NativeArray<byte>(data.Count, Allocator.Temp);
            NativeArray<byte>.Copy(data.Array, data.Offset, buffer, 0, data.Count);

            var writeStatus = Driver.BeginSend(pipeline, connection, out var writer);
            if (writeStatus != (int)StatusCode.Success)
            {
                throw new NetworkException($"Failed to send data: {(StatusCode)writeStatus}");
            }

            writer.WriteBytes(buffer);
            Driver.EndSend(writer);
        }

        protected async UniTask SignInIfNeeded()
        {
            if (Settings.AutoSignInToUnityServices && !AuthenticationService.IsSignedIn)
            {
                await AuthenticationService.SignInAnonymouslyAsync();
            }
        }

        protected async UniTask CreateAndBindNetworkDriver(NetworkSettings networkSettings)
        {
            Driver = NetworkDriver.Create(networkSettings);

            if (Driver.Bind(NetworkEndPoint.AnyIpv4) != 0)
            {
                throw new NetworkException("Failed to bind to local address");
            }

            while (!Driver.Bound)
            {
                Driver.ScheduleUpdate().Complete();

                await UniTask.Yield();
            }
        }

        protected void CreateNetworkPipelines()
        {
            ReliablePipeline = Driver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));
            UnreliablePipeline = Driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));
        }

        protected void PushEvents()
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
        protected virtual void BeginTrackingConnection(UnityNetworkConnection connection)
        {
            connections.Add(connection.InternalId, connection);
        }

        /// <summary>
        /// Notify event listeners that the connection is ready.
        /// <para/>
        /// Must be called after <see cref="BeginTrackingConnection"/>.
        /// </summary>
        protected virtual void OnConnectionReady(UnityNetworkConnection connection)
        {
            connectionEventQueue.Enqueue(new TransportConnectionStatusEventArgs(connection.InternalId, RemoteConnectionStatus.Started));
        }

        protected virtual void OnConnectionStopped(int connectionId)
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
            var sendType = pipeline == ReliablePipeline ? SendType.Reliable : SendType.Unreliable;

            DataReceived?.Invoke(this, new TransportDataReceivedEventArgs(connection.InternalId, data, sendType));
        }
    }
}
#endif
