namespace Library.Components.StateMachines
{
    using Automatonymous;
    using Automatonymous.Binders;
    using Contracts;


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
            InstanceState(x => x.CurrentState, Available);

            Initially(
                When(Added)
                    .CopyDataToInstance()
                    .TransitionTo(Available));
        }

        public Event<BookAdded> Added { get; }

        public State Available { get; }
    }


    public static class BookStateMachineExtensions
    {
        public static EventActivityBinder<Book, BookAdded> CopyDataToInstance(this EventActivityBinder<Book, BookAdded> binder)
        {
            return binder.Then(x =>
            {
                x.Instance.DateAdded = x.Data.Timestamp.Date;
                x.Instance.Title = x.Data.Title;
                x.Instance.Isbn = x.Data.Isbn;
            });
        }
    }
}