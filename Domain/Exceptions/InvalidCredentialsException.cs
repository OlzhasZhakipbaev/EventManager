namespace Domain.Exceptions;

public class InvalidCredentialsException : DomainValidationException
{
    public InvalidCredentialsException()
        : base("Неверный логин или пароль.")
    {
    }
}
