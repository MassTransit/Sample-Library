namespace Library.Contracts
{
    using System;


    public interface CheckOutDurationLimitReached
    {
        Guid CheckOutId { get; }

        DateTime DueDate { get; }
    }
}