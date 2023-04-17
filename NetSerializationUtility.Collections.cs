using System;
using System.Collections.Generic;
using LiteNetLib.Utils;

namespace Exanite.Networking
{
    public static partial class NetSerializationUtility
    {
        /// <summary>
        ///     Reads a <see cref="List{T}"/>
        /// </summary>
        public static List<T> GetListWithCount<T>(this NetDataReader reader, List<T> list = null) where T : INetSerializable, new()
        {
            var count = reader.GetInt();

            if (list == null)
            {
                list = new List<T>(count);
            }
            else
            {
                list.Clear();
                list.Capacity = count;
            }

            for (var i = 0; i < count; i++)
            {
                list.Add(reader.Get<T>());
            }

            return list;
        }

        /// <summary>
        ///     Writes a <see cref="List{T}"/>
        /// </summary>
        public static void PutListWithCount<T>(this NetDataWriter writer, List<T> list) where T : INetSerializable, new()
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            writer.Put(list.Count);

            for (var i = 0; i < list.Count; i++)
            {
                writer.Put(list[i]);
            }
        }
    }
}
