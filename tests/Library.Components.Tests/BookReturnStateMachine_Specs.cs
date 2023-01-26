namespace Library.Components.Tests
{
    using System;
    using System.Threading.Tasks;
    using Consumers;
    using Contracts;
    using MassTransit;
    using MassTransit.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using StateMachines;


    public class When_a_book_is_returned
    {
        [Test]
        public async Task Should_request_the_fine_be_charged()
        {
            await using var provider = new ServiceCollection()
                .ConfigureMassTransit(x =>
                {
                    x.AddConsumer<ChargeFineConsumer>();
                    x.AddSagaStateMachine<BookReturnStateMachine, BookReturn>();
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var checkOutId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            var now = DateTime.UtcNow;

            await harness.Bus.Publish<BookReturned>(new
            {
                CheckOutId = checkOutId,
                InVar.Timestamp,
                BookId = bookId,
                MemberId = memberId,
                CheckOutDate = now - TimeSpan.FromDays(28),
                DueDate = now - TimeSpan.FromDays(14),
                ReturnDate = now,
            });

            var sagaHarness = harness.GetSagaStateMachineHarness<BookReturnStateMachine, BookReturn>();

            Guid? existsId = await sagaHarness.Exists(checkOutId, x => x.ChargingFine);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            Assert.IsTrue(await harness.Consumed.Any<ChargeMemberFine>(), "Fine not consumed");

            Assert.IsTrue(await harness.Consumed.Any<FineCharged>(), "Fine charged");
        }
    }


    public class When_a_book_is_returned_and_fails_to_charge_fine
    {
        [Test]
        public async Task Should_handle_the_request_fault()
        {
            await using var provider = new ServiceCollection()
                .ConfigureMassTransit(x =>
                {
                    x.AddConsumer<BadChargeFineConsumer>()
                        .Endpoint(e => e.Name = KebabCaseEndpointNameFormatter.Instance.Consumer<ChargeFineConsumer>());
                    x.AddSagaStateMachine<BookReturnStateMachine, BookReturn>();
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var checkOutId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            var now = DateTime.UtcNow;

            await harness.Bus.Publish<BookReturned>(new
            {
                CheckOutId = checkOutId,
                InVar.Timestamp,
                BookId = bookId,
                MemberId = memberId,
                CheckOutDate = now - TimeSpan.FromDays(28),
                DueDate = now - TimeSpan.FromDays(14),
                ReturnDate = now,
            });

            var sagaHarness = harness.GetSagaStateMachineHarness<BookReturnStateMachine, BookReturn>();

            Guid? existsId = await sagaHarness.Exists(checkOutId, x => x.ChargingFine);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            Assert.IsTrue(await harness.Consumed.Any<ChargeMemberFine>(), "Fine not consumed");

            Assert.IsTrue(await harness.Consumed.Any<Fault<ChargeMemberFine>>(), "Fine charged");

            await harness.Stop();
        }


        class BadChargeFineConsumer :
            IConsumer<ChargeMemberFine>
        {
            public async Task Consume(ConsumeContext<ChargeMemberFine> context)
            {
                await Task.Delay(1000);

                throw new InvalidOperationException("No money!");
            }
        }
    }
}