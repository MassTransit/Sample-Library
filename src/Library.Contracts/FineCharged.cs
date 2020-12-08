namespace Library.Contracts
{
    using System;


    public interface FineCharged
    {
        Guid MemberId { get; }
        decimal Amount { get; }
    }
}