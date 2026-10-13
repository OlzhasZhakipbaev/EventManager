namespace Bookings.Domain.Exceptions;

public sealed class CancelledValidationException : DomainValidationException
{
    public CancelledValidationException(string message)
        : base(message)
    {
    }
}
