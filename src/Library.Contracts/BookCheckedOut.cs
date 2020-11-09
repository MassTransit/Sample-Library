namespace Library.Contracts
{
    using System;


    public interface BookCheckedOut
    {
        Guid CheckOutId { get; }

        DateTime Timestamp { get; }

        Guid MemberId { get; }

        Guid BookId { get; }
    }
}