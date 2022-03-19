namespace Library.Components.StateMachines
{
    using System;
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
            InstanceState(x => x.CurrentState, Requested, Expired, Reserved);

            Event(() => BookReserved, x => x.CorrelateById(m => m.Message.ReservationId));

            Event(() => BookCheckedOut, x => x.CorrelateBy((instance, context) => instance.BookId == context.Message.BookId));

            Schedule(() => ExpirationSchedule, x => x.ExpirationTokenId, x => x.Delay = TimeSpan.FromHours(24));

            Initially(
                When(ReservationRequested)
                    .Then(context =>
                    {
                        context.Saga.Created = context.Message.Timestamp;
                        context.Saga.BookId = context.Message.BookId;
                        context.Saga.MemberId = context.Message.MemberId;
                    })
                    .TransitionTo(Requested),
                When(BookReserved)
                    .Then(context =>
                    {
                        context.Saga.Created = context.Message.Timestamp;
                        context.Saga.BookId = context.Message.BookId;
                        context.Saga.MemberId = context.Message.MemberId;
                        context.Saga.Reserved = context.Message.Timestamp;
                    })
                    .Schedule(ExpirationSchedule, context => context.Init<ReservationExpired>(new { context.Message.ReservationId }),
                        context => context.Message.Duration ?? TimeSpan.FromDays(1))
                    .TransitionTo(Reserved),
                When(ReservationExpired)
                    .Finalize()
            );

            During(Requested,
                When(BookReserved)
                    .Then(context => context.Saga.Reserved = context.Message.Timestamp)
                    .Schedule(ExpirationSchedule, context => context.Init<ReservationExpired>(new { context.Message.ReservationId }),
                        context => context.Message.Duration ?? TimeSpan.FromDays(1))
                    .TransitionTo(Reserved),
                Ignore(ReservationRequested)
            );

            During(Reserved,
                When(BookReserved)
                    .Schedule(ExpirationSchedule, context => context.Init<ReservationExpired>(new { context.Message.ReservationId }),
                        context => context.Message.Duration ?? TimeSpan.FromDays(1)),
                When(ReservationExpired)
                    .PublishReservationCancelled()
                    .Finalize(),
                When(ReservationCancellationRequested)
                    .PublishReservationCancelled()
                    .Unschedule(ExpirationSchedule)
                    .Finalize(),
                When(BookCheckedOut)
                    .Unschedule(ExpirationSchedule)
                    .Finalize(),
                Ignore(ReservationRequested));

            SetCompletedWhenFinalized();
        }

        public State Requested { get; }
        public State Reserved { get; }
        public State Expired { get; }

        public Schedule<Reservation, ReservationExpired> ExpirationSchedule { get; }

        public Event<BookReserved> BookReserved { get; }
        public Event<BookCheckedOut> BookCheckedOut { get; }
        public Event<ReservationRequested> ReservationRequested { get; }
        public Event<ReservationCancellationRequested> ReservationCancellationRequested { get; }
        public Event<ReservationExpired> ReservationExpired { get; }
    }


    public static class ReservationStateMachineExtensions
    {
        public static EventActivityBinder<Reservation, T> PublishReservationCancelled<T>(this EventActivityBinder<Reservation, T> binder)
            where T : class
        {
            return binder.PublishAsync(context => context.Init<BookReservationCanceled>(new
            {
                ReservationId = context.Saga.CorrelationId,
                context.Saga.BookId
            }));
        }
    }
}