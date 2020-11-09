namespace Library.Contracts
{
    using System;


    public interface CheckOutRenewed
    {
        Guid CheckOutId { get; }

        DateTime DueDate { get; }
    }
}