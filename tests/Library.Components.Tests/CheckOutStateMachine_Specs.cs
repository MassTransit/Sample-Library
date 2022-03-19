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

            var now = DateTime.UtcNow;

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

            Guid? existsId = await SagaHarness.Exists(checkOutId, x => x.CheckedOut);
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

            var now = DateTime.UtcNow;

            await TestHarness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = checkOutId,
                BookId = bookId,
                InVar.Timestamp,
                MemberId = NewId.NextGuid()
            });

            Guid? existsId = await SagaHarness.Exists(checkOutId, x => x.CheckedOut);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            IRequestClient<RenewCheckOut> requestClient = TestHarness.GetRequestClient<RenewCheckOut>();

            now = DateTime.UtcNow;

            Response<CheckOutRenewed, CheckOutNotFound> response = await requestClient.GetResponse<CheckOutRenewed, CheckOutNotFound>(new { checkOutId });

            if (response.Is(out Response<CheckOutRenewed> renewed))
                Assert.That(renewed.Message.DueDate, Is.GreaterThanOrEqualTo(now + TimeSpan.FromDays(14)));

            Assert.That(response.Is(out Response<CheckOutNotFound> _), Is.False);
        }

        [Test]
        public async Task Should_not_complete_on_a_missing_checkout()
        {
            var checkOutId = NewId.NextGuid();

            IRequestClient<RenewCheckOut> requestClient = TestHarness.GetRequestClient<RenewCheckOut>();

            Response<CheckOutRenewed, CheckOutNotFound> response = await requestClient.GetResponse<CheckOutRenewed, CheckOutNotFound>(new { checkOutId });

            Assert.That(response.Is(out Response<CheckOutNotFound> _), Is.True);
            Assert.That(response.Is(out Response<CheckOutRenewed> _), Is.False);
        }

        protected override void ConfigureServices(IServiceCollection collection)
        {
            collection.AddSingleton<CheckOutSettings, TestCheckOutSettings>();

            collection.AddScoped<IMemberRegistry, AnyMemberIsValidMemberRegistry>();

            base.ConfigureServices(collection);
        }

        protected override void ConfigureMassTransit(IBusRegistrationConfigurator configurator)
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

            var now = DateTime.UtcNow;
            var checkedOutAt = DateTime.UtcNow;

            await TestHarness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = checkOutId,
                BookId = bookId,
                InVar.Timestamp,
                MemberId = NewId.NextGuid()
            });

            Guid? existsId = await SagaHarness.Exists(checkOutId, x => x.CheckedOut);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            IRequestClient<RenewCheckOut> requestClient = TestHarness.GetRequestClient<RenewCheckOut>();

            Response<CheckOutRenewed, CheckOutNotFound, CheckOutDurationLimitReached> response =
                await requestClient.GetResponse<CheckOutRenewed, CheckOutNotFound, CheckOutDurationLimitReached>(new { checkOutId });

            now = DateTime.UtcNow;

            Assert.That(response.Is(out Response<CheckOutNotFound> _), Is.False);
            Assert.That(response.Is(out Response<CheckOutRenewed> _), Is.False);
            Assert.That(response.Is(out Response<CheckOutDurationLimitReached> limitReached), Is.True);

            Assert.That(limitReached.Message.DueDate, Is.LessThan(now + TimeSpan.FromDays(14)));
            Assert.That(limitReached.Message.DueDate, Is.GreaterThanOrEqualTo(checkedOutAt + TimeSpan.FromDays(13)));
        }

        protected override void ConfigureServices(IServiceCollection collection)
        {
            collection.AddSingleton<CheckOutSettings, TestCheckOutSettings>();

            collection.AddScoped<IMemberRegistry, AnyMemberIsValidMemberRegistry>();

            base.ConfigureServices(collection);
        }

        protected override void ConfigureMassTransit(IBusRegistrationConfigurator configurator)
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


    public class When_a_book_is_checked_out_by_a_member :
        StateMachineTestFixture<CheckOutStateMachine, CheckOut>
    {
        [Test]
        public async Task Should_add_the_book_to_the_member_collection()
        {
            var bookId = NewId.NextGuid();
            var checkOutId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await TestHarness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = checkOutId,
                BookId = bookId,
                InVar.Timestamp,
                MemberId = memberId
            });

            Guid? existsId = await SagaHarness.Exists(checkOutId, x => x.CheckedOut);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            Assert.IsTrue(await TestHarness.Published.Any<AddBookToMemberCollection>(), "Add to collection not published");

            Assert.IsTrue(await TestHarness.Consumed.Any<AddBookToMemberCollection>(), "Add not consumed");

            Assert.IsTrue(await TestHarness.Published.Any<BookAddedToMemberCollection>(), "Add not consumed");

            Assert.IsTrue(await TestHarness.Consumed.Any<BookAddedToMemberCollection>(), "Add not consumed");
        }

        protected override void ConfigureServices(IServiceCollection collection)
        {
            collection.AddSingleton<CheckOutSettings, TestCheckOutSettings>();

            collection.AddScoped<IMemberRegistry, AnyMemberIsValidMemberRegistry>();

            base.ConfigureServices(collection);
        }

        protected override void ConfigureMassTransit(IBusRegistrationConfigurator configurator)
        {
            configurator.AddConsumer<MemberCollectionConsumer>();
        }


        class TestCheckOutSettings :
            CheckOutSettings
        {
            public TimeSpan CheckOutDuration => TimeSpan.FromDays(14);

            public TimeSpan CheckOutDurationLimit => TimeSpan.FromDays(30);
        }
    }


    public class When_a_book_is_checked_out_by_a_member_with_fault :
        StateMachineTestFixture<CheckOutStateMachine, CheckOut>
    {
        [Test]
        public async Task Should_handle_the_fault_message_via_correlation()
        {
            var bookId = NewId.NextGuid();
            var checkOutId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await TestHarness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = checkOutId,
                BookId = bookId,
                InVar.Timestamp,
                MemberId = memberId
            });

            Guid? existsId = await SagaHarness.Exists(checkOutId, x => x.CheckedOut);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            Assert.IsTrue(await TestHarness.Published.Any<AddBookToMemberCollection>(), "Add to collection not published");

            Assert.IsTrue(await TestHarness.Consumed.Any<AddBookToMemberCollection>(), "Add not consumed");

            Assert.IsTrue(await TestHarness.Published.Any<Fault<AddBookToMemberCollection>>(), "Fault not published");

            Assert.IsTrue(await TestHarness.Consumed.Any<Fault<AddBookToMemberCollection>>(), "Fault not consumed");
        }

        protected override void ConfigureServices(IServiceCollection collection)
        {
            collection.AddSingleton<CheckOutSettings, TestCheckOutSettings>();

            collection.AddScoped<IMemberRegistry, AnyMemberIsValidMemberRegistry>();

            base.ConfigureServices(collection);
        }

        protected override void ConfigureMassTransit(IBusRegistrationConfigurator configurator)
        {
            configurator.AddConsumer<BadMemberCollectionConsumer>();
        }


        class BadMemberCollectionConsumer :
            IConsumer<AddBookToMemberCollection>
        {
            public async Task Consume(ConsumeContext<AddBookToMemberCollection> context)
            {
                await Task.Delay(1000);

                throw new InvalidOperationException("The member is overdrawn");
            }
        }


        class TestCheckOutSettings :
            CheckOutSettings
        {
            public TimeSpan CheckOutDuration => TimeSpan.FromDays(14);

            public TimeSpan CheckOutDurationLimit => TimeSpan.FromDays(30);
        }
    }
}