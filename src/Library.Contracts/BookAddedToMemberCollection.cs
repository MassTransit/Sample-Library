namespace Library.Contracts
{
    using System;


    public interface BookAddedToMemberCollection
    {
        Guid BookId { get; }
        Guid MemberId { get; }
    }
}