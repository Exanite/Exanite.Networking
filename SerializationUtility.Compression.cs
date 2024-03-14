using System.Text;
using Exanite.Core.Utilities;
using LiteNetLib.Utils;

namespace Exanite.Networking
{
    public static partial class SerializationUtility
    {
        /// <summary>
        /// Reads a Brotli compressed <see cref="string"/>
        /// </summary>
        public static string GetCompressedString(this NetDataReader reader)
        {
            var length = reader.GetInt();
            var bytes = CompressionUtility.BrotliDecompress(reader.GetBytesSegment(length));

            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Writes a Brotli compressed <see cref="string"/>
        /// </summary>
        public static void PutCompressedString(this NetDataWriter writer, string value)
        {
            var bytes = CompressionUtility.BrotliCompress(Encoding.UTF8.GetBytes(value));

            writer.Put(bytes.Length);
            writer.Put(bytes);
        }
    }
}
