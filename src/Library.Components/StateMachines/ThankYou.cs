namespace Library.Components.StateMachines
{
    using System;
    using MassTransit;


    // ReSharper disable UnusedAutoPropertyAccessor.Global
    public class ThankYou :
        SagaStateMachineInstance
    {
        public int CurrentState { get; set; }

        public Guid BookId { get; set; }
        public Guid MemberId { get; set; }
        public Guid? ReservationId { get; set; }

        public int ThankYouStatus { get; set; }

        public Guid CorrelationId { get; set; }
    }
}