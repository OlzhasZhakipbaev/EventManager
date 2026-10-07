namespace Domain.Exceptions;

public class AccessDeniedException : DomainValidationException
{
    public Guid UserId { get; }
    public string Operation { get; }

    public AccessDeniedException(Guid userId, string operation)
        : base($"У пользователя {userId} нет прав на операцию «{operation}».")
    {
        UserId = userId;
        Operation = operation;
    }
}