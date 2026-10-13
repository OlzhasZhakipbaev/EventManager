namespace Users.Domain.Exceptions;

public sealed class InvalidCredentialsException : DomainValidationException
{
    public InvalidCredentialsException()
        : base("Неверный логин или пароль.")
    {
    }
}
