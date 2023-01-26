namespace Library.Components.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Contracts;
    using MassTransit;
    using MassTransit.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using StateMachines;


    public class When_a_book_is_checked_out_via_reservation
    {
        [Test]
        public async Task Should_handle_in_order()
        {
            await using var provider = new ServiceCollection()
                .ConfigureMassTransit(x =>
                {
                    x.AddSagaStateMachine<ThankYouStateMachine, ThankYou>();
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var sagaHarness = harness.GetSagaStateMachineHarness<ThankYouStateMachine, ThankYou>();

            var reservationId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await harness.Bus.Publish<BookReserved>(new
            {
                bookId,
                memberId,
                reservationId,
                Duration = TimeSpan.FromDays(14),
                InVar.Timestamp
            });

            Assert.IsTrue(await sagaHarness.Consumed.Any<BookReserved>(x => x.Context.Message.BookId == bookId), "Message not consumed by saga");

            Assert.That(await sagaHarness.Created.Any(x => x.Saga.BookId == bookId), "Saga not created");

            ISagaInstance<ThankYou> instance = sagaHarness.Created.Select(x => x.Saga.BookId == bookId).First();

            await harness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = InVar.Id,
                bookId,
                memberId,
                InVar.Timestamp
            });

            Assert.IsTrue(await sagaHarness.Consumed.Any<BookCheckedOut>(x => x.Context.Message.BookId == bookId), "Message not consumed by saga");

            Guid? existsId = await sagaHarness.Exists(instance.Saga.CorrelationId, x => x.Ready);
            Assert.IsTrue(existsId.HasValue, "Saga did not transition to Ready");
        }

        [Test]
        public async Task Should_handle_in_other_order()
        {
            await using var provider = new ServiceCollection()
                .ConfigureMassTransit(x =>
                {
                    x.AddSagaStateMachine<ThankYouStateMachine, ThankYou>();
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var sagaHarness = harness.GetSagaStateMachineHarness<ThankYouStateMachine, ThankYou>();

            var reservationId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await harness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = InVar.Id,
                bookId,
                memberId,
                InVar.Timestamp
            });

            Assert.IsTrue(await sagaHarness.Consumed.Any<BookCheckedOut>(x => x.Context.Message.BookId == bookId), "Message not consumed by saga");

            Assert.That(await sagaHarness.Created.Any(x => x.Saga.BookId == bookId), "Saga not created");

            ISagaInstance<ThankYou> instance = sagaHarness.Created.Select(x => x.Saga.BookId == bookId).First();

            await harness.Bus.Publish<BookReserved>(new
            {
                bookId,
                memberId,
                reservationId,
                Duration = TimeSpan.FromDays(14),
                InVar.Timestamp
            });

            Assert.IsTrue(await sagaHarness.Consumed.Any<BookReserved>(x => x.Context.Message.BookId == bookId), "Message not consumed by saga");

            Guid? existsId = await sagaHarness.Exists(instance.Saga.CorrelationId, x => x.Ready);
            Assert.IsTrue(existsId.HasValue, "Saga did not transition to Ready");
        }
    }
}