namespace Library.Contracts
{
    using System;


    public interface BookReserved
    {
        Guid ReservationId { get; }

        DateTime Timestamp { get; }

        Guid MemberId { get; }

        Guid BookId { get; }
    }
}