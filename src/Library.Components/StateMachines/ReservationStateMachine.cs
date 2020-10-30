namespace Library.Components.StateMachines
{
    using System;
    using Automatonymous;
    using Contracts;
    using MassTransit;


    // ReSharper disable UnassignedGetOnlyAutoProperty MemberCanBePrivate.Global
    public class ReservationStateMachine :
        MassTransitStateMachine<Reservation>
    {
        static ReservationStateMachine()
        {
            MessageContracts.Initialize();
        }

        public ReservationStateMachine()
        {
            InstanceState(x => x.CurrentState, Requested, Expired);

            Event(() => BookReserved, x => x.CorrelateById(m => m.Message.ReservationId));

            Schedule(() => ExpirationSchedule, x => x.ExpirationTokenId, x => x.Delay = TimeSpan.FromHours(24));

            Initially(
                When(ReservationRequested)
                    .Then(context =>
                    {
                        context.Instance.Created = context.Data.Timestamp;
                        context.Instance.BookId = context.Data.BookId;
                        context.Instance.MemberId = context.Data.MemberId;
                    })
                    .TransitionTo(Requested)
            );

            During(Requested,
                When(BookReserved)
                    .Then(context => context.Instance.Reserved = context.Data.Timestamp)
                    .Schedule(ExpirationSchedule, context => context.Init<ReservationExpired>(new {context.Data.ReservationId}))
                    .TransitionTo(Reserved)
            );

            During(Reserved,
                When(ReservationExpired)
                    .PublishAsync(context => context.Init<BookReservationCanceled>(new
                    {
                        context.Data.ReservationId,
                        context.Instance.BookId
                    }))
                    .Finalize()
            );

            SetCompletedWhenFinalized();
        }

        public State Requested { get; }
        public State Reserved { get; }
        public State Expired { get; }

        public Schedule<Reservation, ReservationExpired> ExpirationSchedule { get; }

        public Event<BookReserved> BookReserved { get; }
        public Event<ReservationRequested> ReservationRequested { get; }
        public Event<ReservationExpired> ReservationExpired { get; }
    }
}