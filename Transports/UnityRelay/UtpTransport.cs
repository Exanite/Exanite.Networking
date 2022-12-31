using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Exanite.Core.Events;
using UniDi;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Error;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using UnityEngine;
using UnityNetworkConnection = Unity.Networking.Transport.NetworkConnection;

namespace Exanite.Networking.Transports.UnityRelay
{
    public abstract class UtpTransport : MonoBehaviour, ITransport
    {
        protected NetworkDriver Driver;
        protected NetworkPipeline ReliablePipeline;
        protected NetworkPipeline UnreliablePipeline;

        protected Dictionary<int, UnityNetworkConnection> connections;
        protected List<int> connectionIdsToRemove;

        protected Queue<TransportConnectionStatusEventArgs> eventQueue;

        [Inject] private UtpTransportSettings settings;
        [Inject] protected LazyInject<IRelayService> RelayService;
        [Inject] protected LazyInject<IAuthenticationService> AuthenticationService;

        public UtpTransportSettings Settings => settings;

        public LocalConnectionStatus Status { get; protected set; }

        public event EventHandler<ITransport, TransportReceivedDataEventArgs> ReceivedData;
        public event EventHandler<ITransport, TransportConnectionStatusEventArgs> ConnectionStatus;

        private void Awake()
        {
            connections = new Dictionary<int, UnityNetworkConnection>();
            connectionIdsToRemove = new List<int>();

            eventQueue = new Queue<TransportConnectionStatusEventArgs>();
        }

        private void OnDestroy()
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
                OnConnectionStarted(incomingConnection);
            }

            foreach (var (_, connection) in connections)
            {
                NetworkEvent.Type networkEvent;
                while ((networkEvent = Driver.PopEventForConnection(connection, out var stream, out var pipeline)) != NetworkEvent.Type.Empty)
                {
                    switch (networkEvent)
                    {
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

        public abstract UniTask StartConnection();

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
                eventQueue.Clear();
                connections.Clear();

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
                return;
            }

            // Based off of Unity's Simple Relay Sample (using UTP) package
            var pipeline = sendType == SendType.Reliable ? ReliablePipeline : UnreliablePipeline;

            using var buffer = new NativeArray<byte>(data.Count, Allocator.Temp);
            NativeArray<byte>.Copy(data.Array, data.Offset, buffer, 0, data.Count);

            var writeStatus = Driver.BeginSend(pipeline, connection, out var writer);
            if (writeStatus != (int)StatusCode.Success)
            {
                return;
            }

            writer.WriteBytes(buffer);
            Driver.EndSend(writer);
        }

        protected async UniTask SignInIfNeeded()
        {
            if (Settings.AutoSignInToUnityServices && !AuthenticationService.Value.IsSignedIn)
            {
                await AuthenticationService.Value.SignInAnonymouslyAsync();
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
            ReliablePipeline = Driver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            UnreliablePipeline = Driver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));
        }

        protected void PushEvents()
        {
            while (eventQueue.TryDequeue(out var e))
            {
                ConnectionStatus?.Invoke(this, e);
            }
        }

        protected virtual void OnConnectionStarted(UnityNetworkConnection connection)
        {
            connections.Add(connection.InternalId, connection);

            eventQueue.Enqueue(new TransportConnectionStatusEventArgs(connection.InternalId, RemoteConnectionStatus.Started));
        }

        protected virtual void OnConnectionStopped(int connectionId)
        {
            if (connections.Remove(connectionId))
            {
                eventQueue.Enqueue(new TransportConnectionStatusEventArgs(connectionId, RemoteConnectionStatus.Stopped));
            }
        }

        private void OnNetworkReceive(DataStreamReader stream, UnityNetworkConnection connection, NetworkPipeline pipeline)
        {
            // Based off of Unity's Simple Relay Sample (using UTP) package
            using var buffer = new NativeArray<byte>(stream.Length, Allocator.Temp);
            stream.ReadBytes(buffer);

            var data = new ArraySegment<byte>(buffer.ToArray());
            var sendType = pipeline == ReliablePipeline ? SendType.Reliable : SendType.Unreliable;

            ReceivedData?.Invoke(this, new TransportReceivedDataEventArgs(connection.InternalId, data, sendType));
        }
    }
}
