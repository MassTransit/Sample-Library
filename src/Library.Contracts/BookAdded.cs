namespace Library.Contracts
{
    using System;


    public interface BookAdded
    {
        Guid BookId { get; }

        DateTime Timestamp { get; }

        string Isbn { get; }
        string Title { get; }
    }
}