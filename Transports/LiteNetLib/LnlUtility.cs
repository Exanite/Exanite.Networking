using LiteNetLib;

namespace Exanite.Networking.Transports.LiteNetLib
{
    public static class LnlUtility
    {
        public static DeliveryMethod ToLnlDeliveryMethod(this SendType sendType)
        {
            return sendType == SendType.Unreliable ? DeliveryMethod.Unreliable : DeliveryMethod.ReliableOrdered;
        }

        public static SendType ToDeliveryMethod(this DeliveryMethod deliveryMethod)
        {
            return deliveryMethod == DeliveryMethod.Unreliable ? SendType.Unreliable : SendType.Reliable;
        }
    }
}
