namespace Library.Contracts
{
    using System;


    public interface BookReservationCanceled
    {
        Guid BookId { get; }

        Guid ReservationId { get; }
    }
}