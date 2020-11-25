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

                x.InsertOnInitial = true;
            });

            Event(() => BookCheckedOut, x =>
            {
                x.CorrelateBy((instance, context) => context.Message.BookId == instance.BookId && context.Message.MemberId == instance.MemberId)
                    .SelectId(context => context.MessageId ?? NewId.NextGuid());

                x.InsertOnInitial = true;
            });

            Event(() => GetStatus, x =>
            {
                x.CorrelateBy((instance, context) => context.Message.MemberId == instance.MemberId);

                x.ReadOnly = true;

                x.OnMissingInstance(m => m.ExecuteAsync(context => context.RespondAsync<ThankYouStatus>(new {Status = "Not Found"})));
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

            DuringAny(
                When(GetStatus)
                    .RespondAsync(context => context.Init<ThankYouStatus>(new
                    {
                        context.Instance.MemberId,
                        context.Instance.BookId,
                        Status = ((StateMachine<ThankYou>)this).Accessor.Get(context)
                    })));

            CompositeEvent(() => ReadyToThank, x => x.ThankYouStatus, CompositeEventOptions.IncludeInitial, BookReserved, BookCheckedOut);

            DuringAny(
                When(ReadyToThank)
                    .TransitionTo(Ready));
        }

        public State Active { get; }
        public State Ready { get; }
        public Event<BookReserved> BookReserved { get; }
        public Event<BookCheckedOut> BookCheckedOut { get; }
        public Event<GetThankYouStatus> GetStatus { get; }

        public Event ReadyToThank { get; }
    }
}