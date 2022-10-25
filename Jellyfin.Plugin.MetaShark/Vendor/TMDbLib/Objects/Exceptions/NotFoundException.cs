namespace TMDbLib.Objects.Exceptions
{
    public class NotFoundException : APIException
    {
        public NotFoundException(TMDbStatusMessage statusMessage)
                        : base("The requested item was not found", statusMessage)
        {
        }
    }
}