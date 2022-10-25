using System;

namespace TMDbLib.Objects.Exceptions
{
    public class RequestLimitExceededException : APIException
    {
        public DateTimeOffset? RetryOn { get; }

        public TimeSpan? RetryAfter { get; }

        internal RequestLimitExceededException(TMDbStatusMessage statusMessage, DateTimeOffset? retryOn, TimeSpan? retryAfter)
            : base("You have exceeded the maximum number of request allowed by TMDb please try again later", statusMessage)
        {
            RetryOn = retryOn;
            RetryAfter = retryAfter;
        }
    }
}