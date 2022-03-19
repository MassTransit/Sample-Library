namespace Library.Integration.Tests
{
    using System;
    using System.Threading.Tasks;
    using Components.StateMachines;
    using Contracts;
    using MassTransit;
    using MassTransit.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;


    public class When_a_book_is_checked_out_via_reservation :
        StateMachineTestFixture<ThankYouStateMachine, ThankYou>
    {
        [Test]
        public async Task Should_handle_in_order()
        {
            var sagaId = NewId.NextGuid();

            var reservationId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await TestHarness.Bus.Publish<BookReserved>(new
            {
                bookId,
                memberId,
                reservationId,
                Duration = TimeSpan.FromDays(14),
                InVar.Timestamp,
                __MessageId = sagaId
            });

            var repository = Provider.GetRequiredService<ISagaRepository<ThankYou>>();

            Guid? existsId = await repository.ShouldContainSagaInState(sagaId, Machine, x => x.Active, TestHarness.TestTimeout);
            Assert.IsTrue(existsId.HasValue, "Saga was not created using the MessageId");

            await TestHarness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = InVar.Id,
                bookId,
                memberId,
                InVar.Timestamp,
                __MessageId = sagaId
            });

            existsId = await repository.ShouldContainSagaInState(sagaId, Machine, x => x.Ready, TestHarness.TestTimeout);
            Assert.IsTrue(existsId.HasValue, "Saga did not transition to Ready");
        }

        [Test]
        public async Task Should_handle_in_other_order()
        {
            var sagaId = NewId.NextGuid();

            var reservationId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await TestHarness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = InVar.Id,
                bookId,
                memberId,
                InVar.Timestamp,
                __MessageId = sagaId
            });

            var repository = Provider.GetRequiredService<ISagaRepository<ThankYou>>();

            Guid? existsId = await repository.ShouldContainSagaInState(sagaId, Machine, x => x.Active, TestHarness.TestTimeout);
            Assert.IsTrue(existsId.HasValue, "Saga was not created using the MessageId");

            await TestHarness.Bus.Publish<BookReserved>(new
            {
                bookId,
                memberId,
                reservationId,
                Duration = TimeSpan.FromDays(14),
                InVar.Timestamp,
                __MessageId = sagaId
            });

            existsId = await repository.ShouldContainSagaInState(sagaId, Machine, x => x.Ready, TestHarness.TestTimeout);
            Assert.IsTrue(existsId.HasValue, "Saga did not transition to Ready");
        }

        [Test]
        public async Task Should_handle_status_checks()
        {
            var sagaId = NewId.NextGuid();

            var reservationId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await TestHarness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = InVar.Id,
                bookId,
                memberId,
                InVar.Timestamp,
                __MessageId = sagaId
            });

            var repository = Provider.GetRequiredService<ISagaRepository<ThankYou>>();

            Guid? existsId = await repository.ShouldContainSagaInState(sagaId, Machine, x => x.Active, TestHarness.TestTimeout);
            Assert.IsTrue(existsId.HasValue, "Saga was not created using the MessageId");

            IRequestClient<GetThankYouStatus> client = TestHarness.GetRequestClient<GetThankYouStatus>();

            Response<ThankYouStatus> response = await client.GetResponse<ThankYouStatus>(new { memberId });

            Assert.That(response.Message.Status, Is.EqualTo("Active"));

            existsId = await repository.ShouldContainSagaInState(sagaId, Machine, x => x.Active, TestHarness.TestTimeout);
            Assert.IsTrue(existsId.HasValue, "Saga was not created using the MessageId");

            await TestHarness.Bus.Publish<BookReserved>(new
            {
                bookId,
                memberId,
                reservationId,
                Duration = TimeSpan.FromDays(14),
                InVar.Timestamp,
                __MessageId = sagaId
            });

            existsId = await repository.ShouldContainSagaInState(sagaId, Machine, x => x.Ready, TestHarness.TestTimeout);
            Assert.IsTrue(existsId.HasValue, "Saga did not transition to Ready");

            response = await client.GetResponse<ThankYouStatus>(new { memberId });

            Assert.That(response.Message.Status, Is.EqualTo("Ready"));
        }
    }
}