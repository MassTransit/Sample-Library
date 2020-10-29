namespace Library.Components.Tests
{
    using System.Threading.Tasks;
    using Contracts;
    using MassTransit;
    using MassTransit.Testing;
    using NUnit.Framework;
    using StateMachines;


    public class When_a_book_is_added :
        StateMachineTestFixture<BookStateMachine, Book>
    {
        [Test]
        public async Task Should_create_a_saga_instance()
        {
            var bookId = NewId.NextGuid();

            await TestHarness.Bus.Publish<BookAdded>(new
            {
                BookId = bookId,
                Isbn = "0307969959",
                Title = "Neuromancer"
            });

            Assert.IsTrue(await TestHarness.Consumed.Any<BookAdded>(), "Message not consumed");

            Assert.IsTrue(await SagaHarness.Consumed.Any<BookAdded>(), "Message not consumed by saga");

            Assert.That(await SagaHarness.Created.Any(x => x.CorrelationId == bookId));

            var instance = SagaHarness.Created.ContainsInState(bookId, Machine, Machine.Available);
            Assert.IsNotNull(instance, "Saga instance not found");

            var existsId = await SagaHarness.Exists(bookId, x => x.Available);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");
        }
    }
}