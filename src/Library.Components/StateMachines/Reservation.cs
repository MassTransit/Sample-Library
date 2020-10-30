namespace Library.Components.StateMachines
{
    using System;
    using Automatonymous;


    public class Reservation :
        SagaStateMachineInstance
    {
        public int CurrentState { get; set; }

        public DateTime Created { get; set; }

        public DateTime? Reserved { get; set; }

        public Guid MemberId { get; set; }

        public Guid BookId { get; set; }

        public Guid CorrelationId { get; set; }
    }
}