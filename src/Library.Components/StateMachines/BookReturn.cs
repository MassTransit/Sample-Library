namespace Library.Components.StateMachines
{
    using System;
    using MassTransit;


    public class BookReturn :
        SagaStateMachineInstance
    {
        public int CurrentState { get; set; }

        public Guid BookId { get; set; }
        public Guid MemberId { get; set; }
        public DateTime CheckOutDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime ReturnDate { get; set; }

        public Guid? FineRequestId { get; set; }

        public Guid CorrelationId { get; set; }
    }
}