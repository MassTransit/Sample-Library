namespace Library.Components.StateMachines
{
    using System;
    using MassTransit;


    // ReSharper disable UnusedAutoPropertyAccessor.Global
    public class Book :
        SagaStateMachineInstance
    {
        public int CurrentState { get; set; }

        public DateTime DateAdded { get; set; }

        public string Title { get; set; }
        public string Isbn { get; set; }

        public Guid? ReservationId { get; set; }

        public Guid CorrelationId { get; set; }
    }
}