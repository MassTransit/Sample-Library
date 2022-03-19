namespace Library.Components.StateMachines
{
    using Contracts;
    using MassTransit;


    // ReSharper disable UnassignedGetOnlyAutoProperty MemberCanBePrivate.Global
    public sealed class BookStateMachine :
        MassTransitStateMachine<Book>
    {
        static BookStateMachine()
        {
            MessageContracts.Initialize();
        }

        public BookStateMachine()
        {
            InstanceState(x => x.CurrentState, Available, Reserved);

            Event(() => ReservationRequested, x => x.CorrelateById(m => m.Message.BookId));

            Initially(
                When(Added)
                    .CopyDataToInstance()
                    .TransitionTo(Available));

            During(Available,
                When(ReservationRequested)
                    .Then(context => context.Saga.ReservationId = context.Message.ReservationId)
                    .PublishBookReserved()
                    .TransitionTo(Reserved)
            );

            During(Reserved,
                When(ReservationRequested)
                    .If(context => context.Saga.ReservationId.HasValue && context.Saga.ReservationId.Value == context.Message.ReservationId,
                        x => x.PublishBookReserved())
            );

            During(Reserved,
                When(BookReservationCanceled)
                    .TransitionTo(Available));

            During(Available, Reserved,
                When(BookCheckedOut)
                    // .Then(context => context.Saga.ReservationId = default)
                    .TransitionTo(CheckedOut)
            );
        }

        public Event<BookAdded> Added { get; }
        public Event<BookCheckedOut> BookCheckedOut { get; }
        public Event<BookReservationCanceled> BookReservationCanceled { get; }
        public Event<ReservationRequested> ReservationRequested { get; }

        public State Available { get; }
        public State Reserved { get; }
        public State CheckedOut { get; }
    }


    public static class BookStateMachineExtensions
    {
        public static EventActivityBinder<Book, BookAdded> CopyDataToInstance(this EventActivityBinder<Book, BookAdded> binder)
        {
            return binder.Then(x =>
            {
                x.Saga.DateAdded = x.Message.Timestamp.Date;
                x.Saga.Title = x.Message.Title;
                x.Saga.Isbn = x.Message.Isbn;
            });
        }

        public static EventActivityBinder<Book, ReservationRequested> PublishBookReserved(this EventActivityBinder<Book, ReservationRequested> binder)
        {
            return binder.PublishAsync(context => context.Init<BookReserved>(new
            {
                context.Message.ReservationId,
                context.Message.MemberId,
                context.Message.Duration,
                context.Message.BookId,
                InVar.Timestamp
            }));
        }
    }
}