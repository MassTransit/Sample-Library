namespace Library.Contracts
{
    using System;


    public interface ReservationCancellationRequested
    {
        Guid ReservationId { get; }

        DateTime Timestamp { get; }
    }
}