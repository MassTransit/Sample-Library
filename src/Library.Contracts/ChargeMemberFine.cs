namespace Library.Contracts
{
    using System;


    public interface ChargeMemberFine
    {
        Guid MemberId { get; }
        decimal Amount { get; }
    }
}