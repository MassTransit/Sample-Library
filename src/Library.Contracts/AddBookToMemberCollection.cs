namespace Library.Contracts
{
    using System;


    public interface AddBookToMemberCollection
    {
        Guid BookId { get; }
        Guid MemberId { get; }
    }
}