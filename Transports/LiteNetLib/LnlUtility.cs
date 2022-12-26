using LiteNetLib;

namespace Exanite.Networking.Transports.LiteNetLib
{
    public static class LnlUtility
    {
        public static DeliveryMethod ToDeliveryMethod(this SendType sendType)
        {
            return sendType == SendType.Unreliable ? DeliveryMethod.Unreliable : DeliveryMethod.ReliableOrdered;
        }

        public static SendType ToSendType(this DeliveryMethod deliveryMethod)
        {
            return deliveryMethod == DeliveryMethod.Unreliable ? SendType.Unreliable : SendType.Reliable;
        }
    }
}
