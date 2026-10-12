namespace Domain.Exceptions;

public class ActiveBookingsLimitExceededException : DomainValidationException
{
    public Guid UserId { get; }
    public int Limit { get; }

    public ActiveBookingsLimitExceededException(Guid userId, int limit)
        : base($"Пользователь {userId} превысил лимит активных бронирований: {limit}.")
    {
        UserId = userId;
        Limit = limit;
    }
}