namespace Library.Contracts
{
    using System;


    public interface ThankYouStatus
    {
        Guid MemberId { get; }
        Guid BookId { get; }
        string Status { get; }
    }
}