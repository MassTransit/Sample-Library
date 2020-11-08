namespace Library.Contracts
{
    using System;


    public interface ReservationRequested
    {
        Guid ReservationId { get; }

        DateTime Timestamp { get; }

        TimeSpan? Duration { get; }

        Guid MemberId { get; }

        Guid BookId { get; }
    }
}