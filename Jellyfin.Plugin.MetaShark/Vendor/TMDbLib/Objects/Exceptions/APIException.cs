namespace TMDbLib.Objects.Exceptions
{
    public class APIException : TMDbException
    {
        public TMDbStatusMessage StatusMessage { get; }

        public APIException(string message, TMDbStatusMessage statusMessage) : base(message)
        {
            StatusMessage = statusMessage;
        }
    }
}