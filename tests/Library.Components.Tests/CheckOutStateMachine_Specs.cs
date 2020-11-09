namespace Library.Components.Tests
{
    using System;
    using System.Threading.Tasks;
    using Contracts;
    using MassTransit;
    using MassTransit.ExtensionsDependencyInjectionIntegration;
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

            public TimeSpan CheckOutDurationLimit => TimeSpan.FromDays(30);
        }
    }


    public class When_a_checkout_is_renewed :
        StateMachineTestFixture<CheckOutStateMachine, CheckOut>
    {
        [Test]
        public async Task Should_renew_an_existing_checkout()
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

            var existsId = await SagaHarness.Exists(checkOutId, x => x.CheckedOut);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            using var scope = Provider.CreateScope();

            var requestClient = scope.ServiceProvider.GetRequiredService<IRequestClient<RenewCheckOut>>();

            now = DateTime.UtcNow;

            var (renewed, notFound) = await requestClient.GetResponse<CheckOutRenewed, CheckOutNotFound>(new {checkOutId});

            if (renewed.IsCompletedSuccessfully)
            {
                var response = await renewed;
                Assert.That(response.Message.DueDate, Is.GreaterThanOrEqualTo(now + TimeSpan.FromDays(14)));
            }

            Assert.That(notFound.IsCompletedSuccessfully, Is.False);
        }

        [Test]
        public async Task Should_not_complete_on_a_missing_checkout()
        {
            var checkOutId = NewId.NextGuid();

            DateTime now = DateTime.UtcNow;

            using var scope = Provider.CreateScope();

            var requestClient = scope.ServiceProvider.GetRequiredService<IRequestClient<RenewCheckOut>>();

            now = DateTime.UtcNow;

            var (renewed, notFound) = await requestClient.GetResponse<CheckOutRenewed, CheckOutNotFound>(new {checkOutId});

            Assert.That(notFound.IsCompletedSuccessfully, Is.True);
            Assert.That(renewed.IsCompletedSuccessfully, Is.False);
        }

        protected override void ConfigureServices(IServiceCollection collection)
        {
            collection.AddSingleton<CheckOutSettings, TestCheckOutSettings>();

            collection.AddScoped<IMemberRegistry, AnyMemberIsValidMemberRegistry>();

            base.ConfigureServices(collection);
        }

        protected override void ConfigureMassTransit(IServiceCollectionBusConfigurator configurator)
        {
            configurator.AddRequestClient<RenewCheckOut>();

            base.ConfigureMassTransit(configurator);
        }


        class TestCheckOutSettings :
            CheckOutSettings
        {
            public TimeSpan CheckOutDuration => TimeSpan.FromDays(14);
            public TimeSpan CheckOutDurationLimit => TimeSpan.FromDays(30);
        }
    }


    public class When_a_checkout_is_renewed_past_the_limit :
        StateMachineTestFixture<CheckOutStateMachine, CheckOut>
    {
        [Test]
        public async Task Should_renew_an_existing_checkout_up_to_the_limit()
        {
            var bookId = NewId.NextGuid();
            var checkOutId = NewId.NextGuid();

            DateTime now = DateTime.UtcNow;
            DateTime checkedOutAt = DateTime.UtcNow;

            await TestHarness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = checkOutId,
                BookId = bookId,
                InVar.Timestamp,
                MemberId = NewId.NextGuid()
            });

            var existsId = await SagaHarness.Exists(checkOutId, x => x.CheckedOut);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            using var scope = Provider.CreateScope();

            var requestClient = scope.ServiceProvider.GetRequiredService<IRequestClient<RenewCheckOut>>();

            now = DateTime.UtcNow;

            using var request = requestClient.Create(new {checkOutId});

            var renewed = request.GetResponse<CheckOutRenewed>(false);
            var notFound = request.GetResponse<CheckOutNotFound>(false);
            var limitReached = request.GetResponse<CheckOutDurationLimitReached>();

            Task whenAny = await Task.WhenAny(renewed, notFound, limitReached);

            Assert.That(notFound.IsCompletedSuccessfully, Is.False);
            Assert.That(renewed.IsCompletedSuccessfully, Is.False);
            Assert.That(limitReached.IsCompletedSuccessfully, Is.True);

            if (limitReached.IsCompletedSuccessfully)
            {
                var response = await limitReached;
                Assert.That(response.Message.DueDate, Is.LessThan(now + TimeSpan.FromDays(14)));
                Assert.That(response.Message.DueDate, Is.GreaterThanOrEqualTo(checkedOutAt + TimeSpan.FromDays(13)));
            }
        }

        protected override void ConfigureServices(IServiceCollection collection)
        {
            collection.AddSingleton<CheckOutSettings, TestCheckOutSettings>();

            collection.AddScoped<IMemberRegistry, AnyMemberIsValidMemberRegistry>();

            base.ConfigureServices(collection);
        }

        protected override void ConfigureMassTransit(IServiceCollectionBusConfigurator configurator)
        {
            configurator.AddRequestClient<RenewCheckOut>();

            base.ConfigureMassTransit(configurator);
        }


        class TestCheckOutSettings :
            CheckOutSettings
        {
            public TimeSpan CheckOutDuration => TimeSpan.FromDays(14);
            public TimeSpan CheckOutDurationLimit => TimeSpan.FromDays(13);
        }
    }
}