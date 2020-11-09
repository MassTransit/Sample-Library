namespace Library.Components.StateMachines
{
    using Automatonymous;
    using Contracts;


    // ReSharper disable UnassignedGetOnlyAutoProperty MemberCanBePrivate.Global
    public class CheckOutStateMachine :
        MassTransitStateMachine<CheckOut>
    {
        public CheckOutStateMachine(CheckOutSettings settings)
        {
            Event(() => BookCheckedOut, x => x.CorrelateById(m => m.Message.CheckOutId));

            InstanceState(x => x.CurrentState, CheckedOut);

            Initially(
                When(BookCheckedOut)
                    .Then(context =>
                    {
                        context.Instance.BookId = context.Data.BookId;
                        context.Instance.MemberId = context.Data.MemberId;
                        context.Instance.CheckOutDate = context.Data.Timestamp;
                        context.Instance.DueDate = context.Instance.CheckOutDate + settings.CheckOutDuration;
                    })
                    .Activity(x => x.OfInstanceType<NotifyMemberActivity>())
                    .TransitionTo(CheckedOut));
        }

        public Event<BookCheckedOut> BookCheckedOut { get; }

        public State CheckedOut { get; }
    }
}