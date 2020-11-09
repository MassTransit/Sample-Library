namespace Library.Contracts
{
    using System;


    public interface NotifyMemberDueDate
    {
        Guid MemberId { get; }
        DateTime DueDate { get; }
    }
}