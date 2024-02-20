namespace Exanite.Networking.Channels
{
    public static class ChanneledNetworkExtensions
    {
        public static INetworkChannel<T> CreateChannel<T>(this IChanneledNetwork network, string key, SendType sendType) where T : INetworkSerializable, new()
        {
            return network.CreateChannel(key, new T(), sendType);
        }

        public static INetworkChannel<T> CreateChannel<T>(this IChanneledNetwork network, string key, SendType sendType, MessageHandler<T> handler) where T : INetworkSerializable, new()
        {
            var channel = network.CreateChannel(key, new T(), sendType);
            channel.RegisterHandler(handler);

            return channel;
        }

        public static INetworkChannel<T> CreateChannel<T>(this IChanneledNetwork network, string key, T packet, SendType sendType, MessageHandler<T> handler) where T : INetworkSerializable
        {
            var channel = network.CreateChannel(key, packet, sendType);
            channel.RegisterHandler(handler);

            return channel;
        }
    }
}
