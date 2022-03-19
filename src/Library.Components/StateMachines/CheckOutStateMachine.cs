namespace Library.Components.StateMachines
{
    using System;
    using Contracts;
    using MassTransit;


    // ReSharper disable UnassignedGetOnlyAutoProperty MemberCanBePrivate.Global
    public class CheckOutStateMachine :
        MassTransitStateMachine<CheckOut>
    {
        static CheckOutStateMachine()
        {
            MessageContracts.Initialize();
        }

        public CheckOutStateMachine(CheckOutSettings settings)
        {
            Event(() => BookCheckedOut, x => x.CorrelateById(m => m.Message.CheckOutId));

            Event(() => AddedToCollection, x => x.CorrelateBy((instance, context) =>
                instance.BookId == context.Message.BookId && instance.MemberId == context.Message.MemberId));

            Event(() => AddToCollectionFaulted, x => x.CorrelateBy((instance, context) =>
                instance.BookId == context.Message.Message.BookId && instance.MemberId == context.Message.Message.MemberId));

            Event(() => RenewCheckOutRequested, x => x.OnMissingInstance(m => m.ExecuteAsync(a => a.RespondAsync<CheckOutNotFound>(a.Message))));

            InstanceState(x => x.CurrentState, CheckedOut);

            Initially(
                When(BookCheckedOut)
                    .Then(context =>
                    {
                        context.Saga.BookId = context.Message.BookId;
                        context.Saga.MemberId = context.Message.MemberId;
                        context.Saga.CheckOutDate = context.Message.Timestamp;
                        context.Saga.DueDate = context.Saga.CheckOutDate + settings.CheckOutDuration;
                    })
                    .Activity(x => x.OfInstanceType<NotifyMemberActivity>())
                    .PublishAsync(x => x.Init<AddBookToMemberCollection>(new
                    {
                        x.Saga.MemberId,
                        x.Saga.BookId
                    }))
                    .TransitionTo(CheckedOut));

            During(CheckedOut,
                When(RenewCheckOutRequested)
                    .Then(context =>
                    {
                        context.Saga.DueDate = DateTime.UtcNow + settings.CheckOutDuration;
                    })
                    .IfElse(context => context.Saga.DueDate > context.Saga.CheckOutDate + settings.CheckOutDurationLimit,
                        exceeded => exceeded
                            .Then(context => context.Saga.DueDate = context.Saga.CheckOutDate + settings.CheckOutDurationLimit)
                            .RespondAsync(context => context.Init<CheckOutDurationLimitReached>(new
                            {
                                context.Message.CheckOutId,
                                context.Saga.DueDate
                            })),
                        otherwise => otherwise
                            .Activity(x => x.OfInstanceType<NotifyMemberActivity>())
                            .RespondAsync(context => context.Init<CheckOutRenewed>(new
                            {
                                context.Message.CheckOutId,
                                context.Saga.DueDate
                            })))
            );

            DuringAny(When(AddedToCollection)
                .Then(context =>
                {
                }));

            DuringAny(When(AddToCollectionFaulted)
                .Then(context =>
                {
                    Console.WriteLine("Add to collection faulted");
                }));
        }

        public Event<BookCheckedOut> BookCheckedOut { get; }
        public Event<RenewCheckOut> RenewCheckOutRequested { get; }

        public Event<BookAddedToMemberCollection> AddedToCollection { get; }
        public Event<Fault<AddBookToMemberCollection>> AddToCollectionFaulted { get; }

        public State CheckedOut { get; }
    }
}