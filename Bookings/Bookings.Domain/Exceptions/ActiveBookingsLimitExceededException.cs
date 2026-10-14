namespace Bookings.Domain.Exceptions;

public sealed class ActiveBookingsLimitExceededException : DomainValidationException
{
    public Guid UserId { get; }
    public int Limit { get; }

    public ActiveBookingsLimitExceededException(Guid userId, int limit)
        : base($"У пользователя {userId} уже {limit} активных бронирований.")
    {
        UserId = userId;
        Limit = limit;
    }
}
