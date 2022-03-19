namespace Library.Components.StateMachines
{
    using System;
    using Consumers;
    using Contracts;
    using MassTransit;


    // ReSharper disable UnassignedGetOnlyAutoProperty MemberCanBePrivate.Global
    public class BookReturnStateMachine :
        MassTransitStateMachine<BookReturn>
    {
        static BookReturnStateMachine()
        {
            MessageContracts.Initialize();
        }

        public BookReturnStateMachine(IEndpointNameFormatter formatter)
        {
            Event(() => BookReturned, x => x.CorrelateById(m => m.Message.CheckOutId));

            Request(() => ChargeFine, x => x.FineRequestId, x =>
            {
                var endpoint = formatter.Consumer<ChargeFineConsumer>();

                x.ServiceAddress = new Uri($"queue:{endpoint}");
                x.Timeout = TimeSpan.FromSeconds(10);
            });

            InstanceState(x => x.CurrentState, ChargingFine, Complete);

            Initially(
                When(BookReturned)
                    .Then(context =>
                    {
                        context.Saga.BookId = context.Message.BookId;
                        context.Saga.MemberId = context.Message.MemberId;
                        context.Saga.CheckOutDate = context.Message.Timestamp;
                        context.Saga.DueDate = context.Message.DueDate;
                        context.Saga.ReturnDate = context.Message.ReturnDate;
                    })
                    .IfElse(context => context.Saga.ReturnDate > context.Saga.DueDate,
                        late => late.Request(ChargeFine, context => context.Init<ChargeMemberFine>(new
                        {
                            context.Saga.MemberId,
                            Amount = 123.45m
                        })).TransitionTo(ChargingFine),
                        onTime => onTime.TransitionTo(Complete)));


            During(ChargingFine,
                When(ChargeFine.Completed)
                    .TransitionTo(Complete),
                When(ChargeFine.Faulted)
                    .TransitionTo(FailedToFineMember),
                When(ChargeFine.TimeoutExpired)
                    .TransitionTo(FailedToFineMember));
        }

        public Event<BookReturned> BookReturned { get; }

        public State ChargingFine { get; }
        public State FailedToFineMember { get; }
        public State Complete { get; }

        public Request<BookReturn, ChargeMemberFine, FineCharged> ChargeFine { get; }
    }
}