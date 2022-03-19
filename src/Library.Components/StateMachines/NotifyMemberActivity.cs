namespace Library.Components.StateMachines
{
    using System;
    using System.Threading.Tasks;
    using Contracts;
    using MassTransit;
    using Services;


    public class NotifyMemberActivity :
        IStateMachineActivity<CheckOut>
    {
        readonly IMemberRegistry _memberRegistry;

        public NotifyMemberActivity(IMemberRegistry memberRegistry)
        {
            _memberRegistry = memberRegistry;
        }

        public void Probe(ProbeContext context)
        {
            context.CreateScope("notifyMember");
        }

        public void Accept(StateMachineVisitor visitor)
        {
            visitor.Visit(this);
        }

        public async Task Execute(BehaviorContext<CheckOut> context, IBehavior<CheckOut> next)
        {
            await Execute(context);

            await next.Execute(context);
        }

        public async Task Execute<T>(BehaviorContext<CheckOut, T> context, IBehavior<CheckOut, T> next)
            where T : class
        {
            await Execute(context);

            await next.Execute(context);
        }

        public Task Faulted<TException>(BehaviorExceptionContext<CheckOut, TException> context, IBehavior<CheckOut> next)
            where TException : Exception
        {
            return next.Faulted(context);
        }

        public Task Faulted<T, TException>(BehaviorExceptionContext<CheckOut, T, TException> context, IBehavior<CheckOut, T> next)
            where TException : Exception
            where T : class
        {
            return next.Faulted(context);
        }

        async Task Execute(BehaviorContext<CheckOut> context)
        {
            var isValid = await _memberRegistry.IsMemberValid(context.Saga.MemberId);
            if (!isValid)
                throw new InvalidOperationException("Invalid memberId");

            var consumeContext = context.GetPayload<ConsumeContext>();

            await consumeContext.Publish<NotifyMemberDueDate>(new
            {
                context.Saga.MemberId,
                context.Saga.DueDate
            });
        }
    }
}