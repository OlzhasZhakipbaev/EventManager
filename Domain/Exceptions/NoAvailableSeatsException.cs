namespace Domain.Exceptions;

public sealed class NoAvailableSeatsException : Exception
{
    public NoAvailableSeatsException(string message)
        : base(message)
    {
    }
}
