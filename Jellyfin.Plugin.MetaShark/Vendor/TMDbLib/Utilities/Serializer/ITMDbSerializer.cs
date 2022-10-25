using System;
using System.IO;

namespace TMDbLib.Utilities.Serializer
{
    public interface ITMDbSerializer
    {
        void Serialize(Stream target, object obj, Type type);
        object Deserialize(Stream source, Type type);
    }
}