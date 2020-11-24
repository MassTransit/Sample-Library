namespace Library.Components.StateMachines
{
    using Automatonymous;
    using Contracts;
    using MassTransit;


    // ReSharper disable UnassignedGetOnlyAutoProperty MemberCanBePrivate.Global
    public sealed class ThankYouStateMachine :
        MassTransitStateMachine<ThankYou>
    {
        public ThankYouStateMachine()
        {
            InstanceState(x => x.CurrentState, Active);

            Event(() => BookReserved, x =>
            {
                x.CorrelateBy((instance, context) => context.Message.BookId == instance.BookId && context.Message.MemberId == instance.MemberId)
                    .SelectId(context => context.MessageId ?? NewId.NextGuid());
            });

            Event(() => BookCheckedOut, x =>
            {
                x.CorrelateBy((instance, context) => context.Message.BookId == instance.BookId && context.Message.MemberId == instance.MemberId)
                    .SelectId(context => context.MessageId ?? NewId.NextGuid());
            });

            Initially(
                When(BookReserved)
                    .Then(context =>
                    {
                        context.Instance.BookId = context.Data.BookId;
                        context.Instance.MemberId = context.Data.MemberId;

                        context.Instance.ReservationId = context.Data.ReservationId;
                    })
                    .TransitionTo(Active),
                When(BookCheckedOut)
                    .Then(context =>
                    {
                        context.Instance.BookId = context.Data.BookId;
                        context.Instance.MemberId = context.Data.MemberId;
                    })
                    .TransitionTo(Active)
            );

            During(Active,
                When(BookReserved)
                    .Then(context =>
                    {
                        context.Instance.ReservationId = context.Data.ReservationId;
                    }),
                Ignore(BookCheckedOut)
            );

            CompositeEvent(() => ReadyToThank, x => x.ThankYouStatus, CompositeEventOptions.IncludeInitial, BookReserved, BookCheckedOut);

            DuringAny(
                When(ReadyToThank)
                    .TransitionTo(Ready));
        }

        public State Active { get; }
        public State Ready { get; }
        public Event<BookReserved> BookReserved { get; }
        public Event<BookCheckedOut> BookCheckedOut { get; }

        public Event ReadyToThank { get; }
    }
}