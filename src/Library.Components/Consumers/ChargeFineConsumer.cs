namespace Library.Components.Consumers
{
    using System.Threading.Tasks;
    using Contracts;
    using MassTransit;


    public class ChargeFineConsumer :
        IConsumer<ChargeMemberFine>
    {
        public async Task Consume(ConsumeContext<ChargeMemberFine> context)
        {
            await Task.Delay(1000);

            await context.RespondAsync<FineCharged>(context.Message);
        }
    }
}