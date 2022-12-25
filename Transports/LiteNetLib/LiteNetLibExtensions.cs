using LnlDeliveryMethod = LiteNetLib.DeliveryMethod;

namespace Exanite.Networking.Transports.LiteNetLib
{
    public static class LiteNetLibExtensions
    {
        public static LnlDeliveryMethod ToLnlDeliveryMethod(this DeliveryMethod deliveryMethod)
        {
            return deliveryMethod == DeliveryMethod.Unreliable ? LnlDeliveryMethod.Unreliable : LnlDeliveryMethod.ReliableOrdered;
        }

        public static DeliveryMethod ToDeliveryMethod(this LnlDeliveryMethod deliveryMethod)
        {
            return deliveryMethod == LnlDeliveryMethod.Unreliable ? DeliveryMethod.Unreliable : DeliveryMethod.Reliable;
        }
    }
}
