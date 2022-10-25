using System;

namespace TMDbLib.Objects.Exceptions
{
    public class TMDbException : Exception
    {
        public TMDbException(string message)
                : base(message)
        {
        }
    }
}