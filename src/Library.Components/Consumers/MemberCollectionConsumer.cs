namespace Library.Components.Consumers
{
    using System.Threading.Tasks;
    using Contracts;
    using MassTransit;


    public class MemberCollectionConsumer :
        IConsumer<AddBookToMemberCollection>
    {
        public async Task Consume(ConsumeContext<AddBookToMemberCollection> context)
        {
            await Task.Delay(1000);

            await context.Publish<BookAddedToMemberCollection>(context.Message);
        }
    }
}