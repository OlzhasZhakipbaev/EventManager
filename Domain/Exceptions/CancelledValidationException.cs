namespace Domain.Exceptions;

public class CancelledValidationException : DomainValidationException
{
    public CancelledValidationException(string message) : base(message)
    {
    }
}
