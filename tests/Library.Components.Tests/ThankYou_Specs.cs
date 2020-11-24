namespace Library.Components.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Contracts;
    using MassTransit;
    using MassTransit.Testing;
    using NUnit.Framework;
    using StateMachines;


    public class When_a_book_is_checked_out_via_reservation :
        StateMachineTestFixture<ThankYouStateMachine, ThankYou>
    {
        [Test]
        public async Task Should_handle_in_order()
        {
            var reservationId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await TestHarness.Bus.Publish<BookReserved>(new
            {
                bookId,
                memberId,
                reservationId,
                Duration = TimeSpan.FromDays(14),
                InVar.Timestamp
            });

            Assert.IsTrue(await SagaHarness.Consumed.Any<BookReserved>(x => x.Context.Message.BookId == bookId), "Message not consumed by saga");

            Assert.That(await SagaHarness.Created.Any(x => x.Saga.BookId == bookId), "Saga not created");

            ISagaInstance<ThankYou> instance = SagaHarness.Created.Select(x => x.Saga.BookId == bookId).First();

            await TestHarness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = InVar.Id,
                bookId,
                memberId,
                InVar.Timestamp
            });

            Assert.IsTrue(await SagaHarness.Consumed.Any<BookCheckedOut>(x => x.Context.Message.BookId == bookId), "Message not consumed by saga");

            Guid? existsId = await SagaHarness.Exists(instance.Saga.CorrelationId, x => x.Ready);
            Assert.IsTrue(existsId.HasValue, "Saga did not transition to Ready");
        }

        [Test]
        public async Task Should_handle_in_other_order()
        {
            var reservationId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await TestHarness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = InVar.Id,
                bookId,
                memberId,
                InVar.Timestamp
            });

            Assert.IsTrue(await SagaHarness.Consumed.Any<BookCheckedOut>(x => x.Context.Message.BookId == bookId), "Message not consumed by saga");

            Assert.That(await SagaHarness.Created.Any(x => x.Saga.BookId == bookId), "Saga not created");

            ISagaInstance<ThankYou> instance = SagaHarness.Created.Select(x => x.Saga.BookId == bookId).First();

            await TestHarness.Bus.Publish<BookReserved>(new
            {
                bookId,
                memberId,
                reservationId,
                Duration = TimeSpan.FromDays(14),
                InVar.Timestamp
            });

            Assert.IsTrue(await SagaHarness.Consumed.Any<BookReserved>(x => x.Context.Message.BookId == bookId), "Message not consumed by saga");

            Guid? existsId = await SagaHarness.Exists(instance.Saga.CorrelationId, x => x.Ready);
            Assert.IsTrue(existsId.HasValue, "Saga did not transition to Ready");
        }
    }
}