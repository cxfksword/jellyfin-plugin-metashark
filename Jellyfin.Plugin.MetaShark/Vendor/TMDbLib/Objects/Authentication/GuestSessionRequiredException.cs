using System;

namespace TMDbLib.Objects.Authentication
{
    public class GuestSessionRequiredException : Exception
    {
        public GuestSessionRequiredException()
            : base("The method you called requires a valid guest or user session to be set on the client object. Please use the 'SetSessionInformation' method to do so.")
        {

        }
    }
}
