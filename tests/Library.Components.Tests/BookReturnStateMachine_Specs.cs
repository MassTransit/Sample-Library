namespace Library.Components.Tests
{
    using System;
    using System.Threading.Tasks;
    using Consumers;
    using Contracts;
    using MassTransit;
    using NUnit.Framework;
    using StateMachines;


    public class When_a_book_is_returned :
        StateMachineTestFixture<BookReturnStateMachine, BookReturn>
    {
        [Test]
        public async Task Should_request_the_fine_be_charged()
        {
            var checkOutId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            var now = DateTime.UtcNow;

            await TestHarness.Bus.Publish<BookReturned>(new
            {
                CheckOutId = checkOutId,
                InVar.Timestamp,
                BookId = bookId,
                MemberId = memberId,
                CheckOutDate = now - TimeSpan.FromDays(28),
                DueDate = now - TimeSpan.FromDays(14),
                ReturnDate = now,
            });

            Guid? existsId = await SagaHarness.Exists(checkOutId, x => x.ChargingFine);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            Assert.IsTrue(await TestHarness.Consumed.Any<ChargeMemberFine>(), "Fine not consumed");

            Assert.IsTrue(await TestHarness.Consumed.Any<FineCharged>(), "Fine charged");
        }

        protected override void ConfigureMassTransit(IBusRegistrationConfigurator configurator)
        {
            configurator.AddConsumer<ChargeFineConsumer>();
        }
    }


    public class When_a_book_is_returned_and_fails_to_charge_fine :
        StateMachineTestFixture<BookReturnStateMachine, BookReturn>
    {
        [Test]
        public async Task Should_handle_the_request_fault()
        {
            var checkOutId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            var now = DateTime.UtcNow;

            await TestHarness.Bus.Publish<BookReturned>(new
            {
                CheckOutId = checkOutId,
                InVar.Timestamp,
                BookId = bookId,
                MemberId = memberId,
                CheckOutDate = now - TimeSpan.FromDays(28),
                DueDate = now - TimeSpan.FromDays(14),
                ReturnDate = now,
            });

            Guid? existsId = await SagaHarness.Exists(checkOutId, x => x.ChargingFine);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            Assert.IsTrue(await TestHarness.Consumed.Any<ChargeMemberFine>(), "Fine not consumed");

            Assert.IsTrue(await TestHarness.Consumed.Any<Fault<ChargeMemberFine>>(), "Fine charged");
        }

        protected override void ConfigureMassTransit(IBusRegistrationConfigurator configurator)
        {
            configurator.AddConsumer<BadChargeFineConsumer>()
                .Endpoint(x => x.Name = KebabCaseEndpointNameFormatter.Instance.Consumer<ChargeFineConsumer>());
        }


        public class BadChargeFineConsumer :
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