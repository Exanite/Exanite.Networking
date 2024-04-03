using System;
using System.Collections.Generic;
using Exanite.Core.Collections;
using LiteNetLib.Utils;
using UnityEngine;

namespace Exanite.Networking.Channels
{
    public class ChanneledNetwork : IPacketHandler, IChanneledNetwork
    {
        private readonly IPacketHandlerNetwork network;

        private readonly NetDataWriter writer = new();
        private readonly NetworkProtocol localProtocol = new();

        private readonly Dictionary<string, NetworkChannel> channelsByKey = new();
        private readonly List<NetworkChannel> channelsById = new();

        private readonly Dictionary<NetworkConnection, ConnectionInfo> connections = new();
        private readonly HashSet<NetworkConnection> readyConnections = new();

        public ChanneledNetwork(int handlerId, IPacketHandlerNetwork network)
        {
            this.network = network;
            HandlerId = handlerId;

            Channels = new ReadOnlyDictionaryWrapper<string, NetworkChannel, INetworkChannel>(channelsByKey);
        }

        public int HandlerId { get; }

        public bool IsServer => network.IsServer;
        public bool IsClient => network.IsClient;

        public LocalConnectionStatus Status { get; private set; }
        public bool IsReady => Status == LocalConnectionStatus.Started;

        public IEnumerable<NetworkConnection> Connections => readyConnections;
        public IReadOnlyDictionary<string, INetworkChannel> Channels { get; }

        public event NetworkStartedEvent NetworkStarted;
        public event NetworkStoppedEvent NetworkStopped;

        public event ConnectionStartedEvent ConnectionStarted;
        public event ConnectionStoppedEvent ConnectionStopped;

        public INetworkChannel<T> CreateChannel<T>(string key, T packet, SendType sendType) where T : INetworkSerializable
        {
            if (channelsByKey.ContainsKey(key))
            {
                throw new ArgumentException($"A network channel with the same key has already been added: {key}", nameof(key));
            }

            var channel = new NetworkChannel<T>(this, key, packet, sendType);
            channel.DataSent += OnChannelDataSent;

            channelsByKey.Add(key, channel);
            if (network.IsServer)
            {
                channelsById.Add(channel);
                channel.Id = channelsById.Count - 1;
            }

            localProtocol.Channels.Add(new NetworkProtocolChannel
            {
                Key = key,
                PacketType = $"{typeof(T).FullName}, {typeof(T).Assembly.GetName().Name}",
            });

            return channel;
        }

        private void OnConnectionStarted(INetwork network, NetworkConnection connection)
        {
            SendChannelList(connection);
        }

        private void OnConnectionStopped(INetwork network, NetworkConnection connection)
        {
            connections.Remove(connection);
            readyConnections.Remove(connection);

            ConnectionStopped?.Invoke(this, connection);
        }

        private void SendChannelList(NetworkConnection connection)
        {
            writer.Reset();
            writer.Put((int)MessageType.ChannelList);

            localProtocol.Serialize(writer);

            network.SendAsPacketHandler(this, connection, writer, SendType.Reliable);
        }

        private void OnChannelListReceived(NetworkConnection connection, NetDataReader reader)
        {
            if (connections.ContainsKey(connection))
            {
                connection.Disconnect();

                return;
            }

            var connectionInfo = new ConnectionInfo();
            connectionInfo.ReceivedProtocol.Deserialize(reader);

            connections.Add(connection, connectionInfo);

            if (!localProtocol.IsCompatible(connectionInfo.ReceivedProtocol))
            {
                Debug.LogWarning($"Disconnecting connection due to incompatible protocol. Connection ID: {connection.Id}");

                connection.Disconnect();

                return;
            }

            if (network.IsServer)
            {
                Server_SendChannelIdAssignment(connection);
                SendReadyToReceive(connection);
            }
        }

        private void Server_SendChannelIdAssignment(NetworkConnection connection)
        {
            writer.Reset();
            writer.Put((int)MessageType.ChannelIdAssignment);

            writer.Put(channelsById.Count);
            foreach (var channel in channelsById)
            {
                writer.Put(channel.Key);
            }

            network.SendAsPacketHandler(this, connection, writer, SendType.Reliable);
        }

        private void Client_OnChannelIdAssignment(NetworkConnection connection, NetDataReader reader)
        {
            var count = reader.GetInt();

            channelsById.Clear();
            channelsById.Capacity = count;

            for (var i = 0; i < count; i++)
            {
                var key = reader.GetString();
                if (channelsByKey.TryGetValue(key, out var channel))
                {
                    channel.Id = i;
                }

                // Null channels are allowed
                channelsById.Add(channel);
            }

            SendReadyToReceive(connection);
            SetStatus(LocalConnectionStatus.Started);
        }

        private void SendReadyToReceive(NetworkConnection connection)
        {
            writer.Reset();
            writer.Put((int)MessageType.ReadyToReceive);

            network.SendAsPacketHandler(this, connection, writer, SendType.Reliable);
        }

        private void OnReadyToReceive(NetworkConnection connection)
        {
            if (connections.TryGetValue(connection, out var info) && !info.IsReady)
            {
                info.IsReady = true;
                readyConnections.Add(connection);

                ConnectionStarted?.Invoke(this, connection);
            }
        }

        private void OnChannelDataSent(NetworkChannelDataSentEventArgs args)
        {
            if (!IsReady)
            {
                throw new InvalidOperationException($"{GetType().Name} is not ready to send.");
            }

            if (!connections.TryGetValue(args.Connection, out var info) || !info.IsReady)
            {
                throw new InvalidOperationException($"The remote connection is not ready to receive. Connection ID: {args.Connection.Id}");
            }

            writer.Reset();
            writer.Put((int)MessageType.ChannelData);

            writer.Put(args.ChannelId);
            writer.Put(args.Writer.Data, 0, args.Writer.Length);

            network.SendAsPacketHandler(this, args.Connection, writer, args.SendType);
        }

        private void OnChannelDataReceived(NetworkConnection connection, NetDataReader reader)
        {
            if (!IsReady)
            {
                Debug.LogWarning($"{GetType().Name} is not ready, but already received data from connection ID '{connection.Id}'. Disconnecting connection ID: {connection.Id}.");
                connection.Disconnect();

                return;
            }

            var channelId = reader.GetInt();
            var channel = channelsById[channelId];
            if (channel == null)
            {
                return;
            }

            channel.OnDataReceived(connection, reader);
        }

        private void SetStatus(LocalConnectionStatus value)
        {
            if (Status == value)
            {
                return;
            }

            Status = value;
            switch (Status)
            {
                case LocalConnectionStatus.Started:
                {
                    NetworkStarted?.Invoke(this);

                    break;
                }
                case LocalConnectionStatus.Stopped:
                {
                    NetworkStopped?.Invoke(this);

                    break;
                }
            }
        }

        void IPacketHandler.OnNetworkStarted(INetwork network)
        {
            network.ConnectionStarted += OnConnectionStarted;
            network.ConnectionStopped += OnConnectionStopped;

            SetStatus(network.IsServer ? LocalConnectionStatus.Started : LocalConnectionStatus.Starting);
        }

        void IPacketHandler.OnNetworkStopped(INetwork network)
        {
            network.ConnectionStarted -= OnConnectionStarted;
            network.ConnectionStopped -= OnConnectionStopped;

            localProtocol.Clear();
            connections.Clear();

            if (network.IsClient)
            {
                foreach (var networkChannel in channelsById)
                {
                    networkChannel.Id = NetworkChannel.InvalidId;
                }

                channelsById.Clear();
            }

            SetStatus(LocalConnectionStatus.Stopped);
        }

        void IPacketHandler.OnReceive(INetwork network, NetworkConnection connection, NetDataReader reader, SendType sendType)
        {
            var messageType = (MessageType)reader.GetInt();
            switch (messageType)
            {
                case MessageType.ChannelList:
                {
                    OnChannelListReceived(connection, reader);

                    break;
                }
                case MessageType.ChannelIdAssignment:
                {
                    Client_OnChannelIdAssignment(connection, reader);

                    break;
                }
                case MessageType.ReadyToReceive:
                {
                    OnReadyToReceive(connection);

                    break;
                }
                case MessageType.ChannelData:
                {
                    OnChannelDataReceived(connection, reader);

                    break;
                }
            }
        }

        private class ConnectionInfo
        {
            public NetworkProtocol ReceivedProtocol { get; } = new();
            public bool IsReady { get; set; }
        }

        private enum MessageType
        {
            /// <summary>
            /// Step 1. Sent by both server and client to confirm protocol compatibility.
            /// </summary>
            ChannelList,

            /// <summary>
            /// Step 2. Sent by server to tell client which channel ids will be used.
            /// </summary>
            ChannelIdAssignment,

            /// <summary>
            /// Step 3. Send by both server and client when they are ready to receive.
            /// </summary>
            ReadyToReceive,

            /// <summary>
            /// Data sent or received by a channel.
            /// </summary>
            ChannelData,
        }
    }
}
