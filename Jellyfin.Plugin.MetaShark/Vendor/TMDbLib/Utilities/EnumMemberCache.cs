using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TMDbLib.Utilities
{
    internal static class EnumMemberCache
    {
        private static readonly Dictionary<Type, Dictionary<object, string>> MemberCache;

        static EnumMemberCache()
        {
            MemberCache = new Dictionary<Type, Dictionary<object, string>>();
        }

        private static Dictionary<object, string> GetOrPrepareCache(Type type)
        {
            if (!type.GetTypeInfo().IsEnum)
                throw new ArgumentException();

            Dictionary<object, string> cache;
            lock (MemberCache)
            {
                if (MemberCache.TryGetValue(type, out cache))
                    return cache;
            }

            cache = new Dictionary<object, string>();

            foreach (FieldInfo fieldInfo in type.GetTypeInfo().DeclaredMembers.OfType<FieldInfo>().Where(s => s.IsStatic))
            {
                object value = fieldInfo.GetValue(null);
                CustomAttributeData attrib = fieldInfo.CustomAttributes.FirstOrDefault(s => s.AttributeType == typeof(EnumValueAttribute));

                if (attrib == null)
                {
                    cache[value] = value.ToString();
                }
                else
                {
                    CustomAttributeTypedArgument arg = attrib.ConstructorArguments.FirstOrDefault();
                    string enumValue = arg.Value as string;

                    cache[value] = enumValue;
                }
            }

            lock (MemberCache)
                MemberCache[type] = cache;

            return cache;
        }

        public static T GetValue<T>(string input)
        {
            Type valueType = typeof(T);
            Dictionary<object, string> cache = GetOrPrepareCache(valueType);

            foreach (KeyValuePair<object, string> pair in cache)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(pair.Value, input))
                {
                    return (T)pair.Key;
                }
            }

            return default;
        }

        public static object GetValue(string input, Type type)
        {
            Dictionary<object, string> cache = GetOrPrepareCache(type);

            foreach (KeyValuePair<object, string> pair in cache)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(pair.Value, input))
                {
                    return pair.Key;
                }
            }

            return null;
        }

        public static string GetString(object value)
        {
            Type valueType = value.GetType();
            Dictionary<object, string> cache = GetOrPrepareCache(valueType);

            string str;
            cache.TryGetValue(value, out str);

            return str;
        }
    }
}