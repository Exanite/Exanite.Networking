using System;
using System.Collections.Generic;
using LiteNetLib.Utils;

namespace Exanite.Networking.Channels
{
    public abstract class NetworkChannel
    {
        public static readonly int InvalidId = -1;

        public string Key { get; protected set; }
        public SendType SendType { get; protected set; }

        public int Id { get; set; } = InvalidId;
        public bool IsReady => Id != InvalidId;

        public abstract void OnDataReceived(NetworkConnection connection, NetDataReader reader);
    }

    public class NetworkChannel<TMessage> : NetworkChannel, INetworkChannel<TMessage> where TMessage : INetworkSerializable
    {
        private readonly NetDataWriter writer = new();
        private readonly List<MessageHandler<TMessage>> messageHandlers = new();

        public NetworkChannel(NetworkChannelManager network, string key, TMessage message, SendType sendType)
        {
            Key = key;
            Network = network;
            Message = message;
            SendType = sendType;
        }

        public TMessage Message { get; set; }

        public INetwork Network { get; }

        public event Action<NetworkChannelDataSentEventArgs> DataSent;

        public void Send(NetworkConnection connection)
        {
            Send(connection, Message);
        }

        public void Send(NetworkConnection connection, TMessage message)
        {
            Write(message);
            SendNoWrite(connection);
        }

        public void Write()
        {
            Write(Message);
        }

        public void Write(TMessage message)
        {
            writer.Reset();
            message.Serialize(writer);
        }

        public void SendNoWrite(NetworkConnection connection)
        {
            DataSent?.Invoke(new NetworkChannelDataSentEventArgs(Id, connection, writer, SendType));
        }

        public void RegisterHandler(MessageHandler<TMessage> handler)
        {
            messageHandlers.Add(handler);
        }

        public override void OnDataReceived(NetworkConnection connection, NetDataReader reader)
        {
            Message.Deserialize(reader);

            foreach (var messageHandler in messageHandlers)
            {
                messageHandler.Invoke(connection, Message);
            }
        }
    }
}
