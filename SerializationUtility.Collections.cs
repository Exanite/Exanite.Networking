using System;
using System.Collections.Generic;
using LiteNetLib.Utils;

namespace Exanite.Networking
{
    public static partial class SerializationUtility
    {
        /// <summary>
        /// Reads a <see cref="List{T}"/>
        /// </summary>
        public static List<T> GetListWithCount<T>(this NetDataReader reader, List<T> list = null) where T : INetworkSerializable, new()
        {
            var count = reader.GetInt();

            if (list == null)
            {
                list = new List<T>(count);
            }

            list.Clear();

            // List.EnsureCapacity does not exist in Unity 2021.3.18, which is the Unity version this library is developed against
            if (list.Capacity < count)
            {
                list.Capacity = count;
            }

            for (var i = 0; i < count; i++)
            {
                var value = new T();
                value.Deserialize(reader);

                list.Add(value);
            }

            return list;
        }

        /// <summary>
        /// Writes a <see cref="List{T}"/>
        /// </summary>
        public static void PutListWithCount<T>(this NetDataWriter writer, List<T> list) where T : INetworkSerializable, new()
        {
            if (list == null)
            {
                throw new ArgumentNullException(nameof(list));
            }

            writer.Put(list.Count);

            for (var i = 0; i < list.Count; i++)
            {
                list[i].Serialize(writer);
            }
        }
    }
}
