using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TMDbLib.Utilities
{
    public static class EnumExtensions
    {
        public static string GetDescription<T>(this T enumerationValue) where T : struct
        {
            Type type = enumerationValue.GetType();
            TypeInfo typeInfo = type.GetTypeInfo();

            if (!typeInfo.IsEnum)
            {
                throw new ArgumentException("EnumerationValue must be of Enum type", nameof(enumerationValue));
            }

            IEnumerable<MemberInfo> members = typeof(T).GetTypeInfo().DeclaredMembers;

            string requestedName = enumerationValue.ToString();

            // Tries to find a DisplayAttribute for a potential friendly name for the enum
            foreach (MemberInfo member in members)
            {
                if (member.Name != requestedName)
                    continue;

                foreach (CustomAttributeData attributeData in member.CustomAttributes)
                {
                    if (attributeData.AttributeType != typeof(EnumValueAttribute))
                        continue;

                    // Pull out the Value
                    if (!attributeData.ConstructorArguments.Any())
                        break;

                    CustomAttributeTypedArgument argument = attributeData.ConstructorArguments.First();
                    string value = argument.Value as string;
                    return value;
                }

                break;
            }

            // If we have no description attribute, just return the ToString of the enum
            return requestedName;
        }
    }
}
