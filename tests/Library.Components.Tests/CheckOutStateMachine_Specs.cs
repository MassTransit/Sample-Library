namespace Library.Components.Tests
{
    using System;
    using System.Threading.Tasks;
    using Contracts;
    using MassTransit;
    using MassTransit.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using Services;
    using StateMachines;


    public class When_a_book_is_checked_out_checkout :
        StateMachineTestFixture<CheckOutStateMachine, CheckOut>
    {
        [Test]
        public async Task Should_create_a_saga_instance()
        {
            var bookId = NewId.NextGuid();
            var checkOutId = NewId.NextGuid();

            DateTime now = DateTime.UtcNow;

            await TestHarness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = checkOutId,
                BookId = bookId,
                InVar.Timestamp,
                MemberId = NewId.NextGuid()
            });

            Assert.IsTrue(await TestHarness.Consumed.Any<BookCheckedOut>(), "Message not consumed");

            Assert.IsTrue(await SagaHarness.Consumed.Any<BookCheckedOut>(), "Message not consumed by saga");

            Assert.That(await SagaHarness.Created.Any(x => x.CorrelationId == checkOutId));

            var instance = SagaHarness.Created.ContainsInState(checkOutId, Machine, Machine.CheckedOut);
            Assert.IsNotNull(instance, "Saga instance not found");

            Assert.That(instance.DueDate, Is.GreaterThanOrEqualTo(now + TimeSpan.FromDays(14)));

            var existsId = await SagaHarness.Exists(checkOutId, x => x.CheckedOut);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            Assert.IsTrue(await TestHarness.Published.Any<NotifyMemberDueDate>(), "Due Date Event Not Published");
        }

        protected override void ConfigureServices(IServiceCollection collection)
        {
            collection.AddSingleton<CheckOutSettings, TestCheckOutSettings>();

            collection.AddScoped<IMemberRegistry, AnyMemberIsValidMemberRegistry>();

            base.ConfigureServices(collection);
        }


        class TestCheckOutSettings :
            CheckOutSettings
        {
            public TimeSpan CheckOutDuration => TimeSpan.FromDays(14);
        }
    }
}