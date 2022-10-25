using System;

namespace TMDbLib.Utilities
{
    [AttributeUsage(AttributeTargets.Field)]
    public class EnumValueAttribute : Attribute
    {
        public EnumValueAttribute(string value)
        {
            Value = value;
        }

        public string Value { get; }
    }
}