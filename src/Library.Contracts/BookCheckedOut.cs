namespace Library.Contracts
{
    using System;


    public interface BookCheckedOut
    {
        DateTime Timestamp { get; }

        Guid MemberId { get; }

        Guid BookId { get; }
    }
}