using LiteNetLib.Utils;
using UnityEngine;

namespace Exanite.Networking
{
    public static partial class SerializationUtility
    {
        // Methods are from the link below, adapted to work for LiteNetLib
        // https://github.com/LukeStampfli/DarkriftSerializationExtensions/blob/master/DarkriftSerializationExtensions/DarkriftSerializationExtensions/SerializationExtensions.cs

        /// <summary>
        /// Reads a <see cref="Vector3"/> (12 bytes)
        /// </summary>
        public static Vector3 GetVector3(this NetDataReader reader)
        {
            return new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());
        }

        /// <summary>
        /// Reads a <see cref="Vector2"/> (8 bytes)
        /// </summary>
        public static Vector2 GetVector2(this NetDataReader reader)
        {
            return new Vector2(reader.GetFloat(), reader.GetFloat());
        }

        /// <summary>
        /// Reads a <see cref="Quaternion"/> (12 bytes)
        /// </summary>
        public static Quaternion GetQuaternion(this NetDataReader reader)
        {
            var x = reader.GetFloat();
            var y = reader.GetFloat();
            var z = reader.GetFloat();
            var w = Mathf.Sqrt(1f - (x * x + y * y + z * z));

            return new Quaternion(x, y, z, w);
        }

        /// <summary>
        /// Writes a <see cref="Vector3"/> (12 bytes)
        /// </summary>
        public static void Put(this NetDataWriter writer, Vector3 value)
        {
            writer.Put(value.x);
            writer.Put(value.y);
            writer.Put(value.z);
        }

        /// <summary>
        /// Writes a <see cref="Vector2"/> (8 bytes)
        /// </summary>
        public static void Put(this NetDataWriter writer, Vector2 value)
        {
            writer.Put(value.x);
            writer.Put(value.y);
        }

        /// <summary>
        /// Writes a <see cref="Quaternion"/> (12 bytes)
        /// </summary>
        public static void Put(this NetDataWriter writer, Quaternion value)
        {
            // (x * x) + (y * y) + (z * z) + (w * w) = 1 => No need to send w
            writer.Put(value.x);
            writer.Put(value.y);
            writer.Put(value.z);
        }
    }
}
