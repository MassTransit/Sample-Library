namespace Library.Contracts
{
    using System;


    public interface ReservationExpired
    {
        Guid ReservationId { get; }
    }
}