namespace Library.Components.StateMachines
{
    using System;
    using Automatonymous;


    public class Book :
        SagaStateMachineInstance
    {
        public int CurrentState { get; set; }

        public DateTime DateAdded { get; set; }

        public string Title { get; set; }
        public string Isbn { get; set; }

        public Guid CorrelationId { get; set; }
    }
}