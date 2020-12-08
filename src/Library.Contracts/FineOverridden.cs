namespace Library.Contracts
{
    using System;


    public interface FineOverridden
    {
        Guid MemberId { get; }
        decimal Amount { get; }
    }
}