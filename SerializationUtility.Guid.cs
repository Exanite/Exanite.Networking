using System;
using LiteNetLib.Utils;

namespace Exanite.Networking
{
    public static partial class SerializationUtility
    {
        /// <summary>
        /// Reads a <see cref="Guid"/> (16 bytes)
        /// </summary>
        public static Guid GetGuid(this NetDataReader reader)
        {
            return new Guid(reader.GetBytesWithLength());
        }

        /// <summary>
        /// Writes a <see cref="Guid"/> (16 bytes)
        /// </summary>
        public static void Put(this NetDataWriter writer, Guid value)
        {
            writer.PutBytesWithLength(value.ToByteArray());
        }
    }
}
