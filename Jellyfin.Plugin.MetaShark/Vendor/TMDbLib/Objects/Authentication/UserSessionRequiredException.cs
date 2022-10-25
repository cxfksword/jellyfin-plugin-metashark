using System;

namespace TMDbLib.Objects.Authentication
{
    public class UserSessionRequiredException : Exception
    {
        public UserSessionRequiredException()
            : base("The method you called requires a valid user session to be set on the client object. Please use the 'SetSessionInformation' method to do so.")
        {

        }
    }
}
