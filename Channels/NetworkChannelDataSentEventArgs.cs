using LiteNetLib.Utils;

namespace Exanite.Networking.Channels
{
    public readonly struct NetworkChannelDataSentEventArgs
    {
        public NetworkChannelDataSentEventArgs(int channelId, NetworkConnection connection, NetDataWriter writer, SendType sendType)
        {
            ChannelId = channelId;
            Connection = connection;
            Writer = writer;
            SendType = sendType;
        }

        public int ChannelId { get; }

        public NetworkConnection Connection { get; }
        public NetDataWriter Writer { get; }
        public SendType SendType { get; }
    }
}
