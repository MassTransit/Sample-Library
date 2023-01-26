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


    public class When_a_book_is_checked_out_checkout
    {
        [Test]
        public async Task Should_create_a_saga_instance()
        {
            await using var provider = new ServiceCollection()
                .ConfigureMassTransit(x =>
                {
                    x.AddSingleton<CheckOutSettings, TestCheckOutSettings>();
                    x.AddScoped<IMemberRegistry, AnyMemberIsValidMemberRegistry>();
                    x.AddSagaStateMachine<CheckOutStateMachine, CheckOut>();
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var bookId = NewId.NextGuid();
            var checkOutId = NewId.NextGuid();

            var now = DateTime.UtcNow;

            await harness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = checkOutId,
                BookId = bookId,
                InVar.Timestamp,
                MemberId = NewId.NextGuid()
            });

            Assert.IsTrue(await harness.Consumed.Any<BookCheckedOut>(), "Message not consumed");

            var sagaHarness = harness.GetSagaStateMachineHarness<CheckOutStateMachine, CheckOut>();

            Assert.IsTrue(await sagaHarness.Consumed.Any<BookCheckedOut>(), "Message not consumed by saga");

            Assert.That(await sagaHarness.Created.Any(x => x.CorrelationId == checkOutId));

            var instance = sagaHarness.Created.ContainsInState(checkOutId, sagaHarness.StateMachine, sagaHarness.StateMachine.CheckedOut);
            Assert.IsNotNull(instance, "Saga instance not found");

            Assert.That(instance.DueDate, Is.GreaterThanOrEqualTo(now + TimeSpan.FromDays(14)));

            Guid? existsId = await sagaHarness.Exists(checkOutId, x => x.CheckedOut);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            Assert.IsTrue(await harness.Published.Any<NotifyMemberDueDate>(), "Due Date Event Not Published");
        }


        class TestCheckOutSettings :
            CheckOutSettings
        {
            public TimeSpan CheckOutDuration => TimeSpan.FromDays(14);
            public TimeSpan CheckOutDurationLimit => TimeSpan.FromDays(30);
        }
    }


    public class When_a_checkout_is_renewed
    {
        [Test]
        public async Task Should_renew_an_existing_checkout()
        {
            await using var provider = new ServiceCollection()
                .ConfigureMassTransit(x =>
                {
                    x.AddSingleton<CheckOutSettings, TestCheckOutSettings>();
                    x.AddScoped<IMemberRegistry, AnyMemberIsValidMemberRegistry>();
                    x.AddSagaStateMachine<CheckOutStateMachine, CheckOut>();
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var bookId = NewId.NextGuid();
            var checkOutId = NewId.NextGuid();

            var now = DateTime.UtcNow;

            await harness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = checkOutId,
                BookId = bookId,
                InVar.Timestamp,
                MemberId = NewId.NextGuid()
            });

            var sagaHarness = harness.GetSagaStateMachineHarness<CheckOutStateMachine, CheckOut>();

            Guid? existsId = await sagaHarness.Exists(checkOutId, x => x.CheckedOut);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            IRequestClient<RenewCheckOut> requestClient = harness.GetRequestClient<RenewCheckOut>();

            now = DateTime.UtcNow;

            Response<CheckOutRenewed, CheckOutNotFound> response = await requestClient.GetResponse<CheckOutRenewed, CheckOutNotFound>(new { checkOutId });

            if (response.Is(out Response<CheckOutRenewed> renewed))
                Assert.That(renewed.Message.DueDate, Is.GreaterThanOrEqualTo(now + TimeSpan.FromDays(14)));

            Assert.That(response.Is(out Response<CheckOutNotFound> _), Is.False);
        }

        [Test]
        public async Task Should_not_complete_on_a_missing_checkout()
        {
            await using var provider = new ServiceCollection()
                .ConfigureMassTransit(x =>
                {
                    x.AddSingleton<CheckOutSettings, TestCheckOutSettings>();
                    x.AddScoped<IMemberRegistry, AnyMemberIsValidMemberRegistry>();
                    x.AddSagaStateMachine<CheckOutStateMachine, CheckOut>();
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var checkOutId = NewId.NextGuid();

            IRequestClient<RenewCheckOut> requestClient = harness.GetRequestClient<RenewCheckOut>();

            Response<CheckOutRenewed, CheckOutNotFound> response = await requestClient.GetResponse<CheckOutRenewed, CheckOutNotFound>(new { checkOutId });

            Assert.That(response.Is(out Response<CheckOutNotFound> _), Is.True);
            Assert.That(response.Is(out Response<CheckOutRenewed> _), Is.False);
        }


        class TestCheckOutSettings :
            CheckOutSettings
        {
            public TimeSpan CheckOutDuration => TimeSpan.FromDays(14);
            public TimeSpan CheckOutDurationLimit => TimeSpan.FromDays(30);
        }
    }


    public class When_a_checkout_is_renewed_past_the_limit
    {
        [Test]
        public async Task Should_renew_an_existing_checkout_up_to_the_limit()
        {
            await using var provider = new ServiceCollection()
                .ConfigureMassTransit(x =>
                {
                    x.AddSingleton<CheckOutSettings, TestCheckOutSettings>();
                    x.AddScoped<IMemberRegistry, AnyMemberIsValidMemberRegistry>();
                    x.AddSagaStateMachine<CheckOutStateMachine, CheckOut>();
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var bookId = NewId.NextGuid();
            var checkOutId = NewId.NextGuid();

            var now = DateTime.UtcNow;
            var checkedOutAt = DateTime.UtcNow;

            await harness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = checkOutId,
                BookId = bookId,
                InVar.Timestamp,
                MemberId = NewId.NextGuid()
            });

            var sagaHarness = harness.GetSagaStateMachineHarness<CheckOutStateMachine, CheckOut>();

            Guid? existsId = await sagaHarness.Exists(checkOutId, x => x.CheckedOut);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            IRequestClient<RenewCheckOut> requestClient = harness.GetRequestClient<RenewCheckOut>();

            Response<CheckOutRenewed, CheckOutNotFound, CheckOutDurationLimitReached> response =
                await requestClient.GetResponse<CheckOutRenewed, CheckOutNotFound, CheckOutDurationLimitReached>(new { checkOutId });

            now = DateTime.UtcNow;

            Assert.That(response.Is(out Response<CheckOutNotFound> _), Is.False);
            Assert.That(response.Is(out Response<CheckOutRenewed> _), Is.False);
            Assert.That(response.Is(out Response<CheckOutDurationLimitReached> limitReached), Is.True);

            Assert.That(limitReached.Message.DueDate, Is.LessThan(now + TimeSpan.FromDays(14)));
            Assert.That(limitReached.Message.DueDate, Is.GreaterThanOrEqualTo(checkedOutAt + TimeSpan.FromDays(13)));
        }


        class TestCheckOutSettings :
            CheckOutSettings
        {
            public TimeSpan CheckOutDuration => TimeSpan.FromDays(14);
            public TimeSpan CheckOutDurationLimit => TimeSpan.FromDays(13);
        }
    }


    public class When_a_book_is_checked_out_by_a_member
    {
        [Test]
        public async Task Should_add_the_book_to_the_member_collection()
        {
            await using var provider = new ServiceCollection()
                .ConfigureMassTransit(x =>
                {
                    x.AddSingleton<CheckOutSettings, TestCheckOutSettings>();
                    x.AddScoped<IMemberRegistry, AnyMemberIsValidMemberRegistry>();
                    x.AddConsumer<MemberCollectionConsumer>();
                    x.AddSagaStateMachine<CheckOutStateMachine, CheckOut>();
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var bookId = NewId.NextGuid();
            var checkOutId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await harness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = checkOutId,
                BookId = bookId,
                InVar.Timestamp,
                MemberId = memberId
            });

            var sagaHarness = harness.GetSagaStateMachineHarness<CheckOutStateMachine, CheckOut>();

            Guid? existsId = await sagaHarness.Exists(checkOutId, x => x.CheckedOut);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            Assert.IsTrue(await harness.Published.Any<AddBookToMemberCollection>(), "Add to collection not published");

            Assert.IsTrue(await harness.Consumed.Any<AddBookToMemberCollection>(), "Add not consumed");

            Assert.IsTrue(await harness.Published.Any<BookAddedToMemberCollection>(), "Add not consumed");

            Assert.IsTrue(await harness.Consumed.Any<BookAddedToMemberCollection>(), "Add not consumed");
        }


        class TestCheckOutSettings :
            CheckOutSettings
        {
            public TimeSpan CheckOutDuration => TimeSpan.FromDays(14);

            public TimeSpan CheckOutDurationLimit => TimeSpan.FromDays(30);
        }
    }


    public class When_a_book_is_checked_out_by_a_member_with_fault
    {
        [Test]
        public async Task Should_handle_the_fault_message_via_correlation()
        {
            await using var provider = new ServiceCollection()
                .ConfigureMassTransit(x =>
                {
                    x.AddSingleton<CheckOutSettings, TestCheckOutSettings>();
                    x.AddScoped<IMemberRegistry, AnyMemberIsValidMemberRegistry>();
                    x.AddConsumer<BadMemberCollectionConsumer>();
                    x.AddSagaStateMachine<CheckOutStateMachine, CheckOut>();
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var bookId = NewId.NextGuid();
            var checkOutId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await harness.Bus.Publish<BookCheckedOut>(new
            {
                CheckOutId = checkOutId,
                BookId = bookId,
                InVar.Timestamp,
                MemberId = memberId
            });

            var sagaHarness = harness.GetSagaStateMachineHarness<CheckOutStateMachine, CheckOut>();

            Guid? existsId = await sagaHarness.Exists(checkOutId, x => x.CheckedOut);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            Assert.IsTrue(await harness.Published.Any<AddBookToMemberCollection>(), "Add to collection not published");

            Assert.IsTrue(await harness.Consumed.Any<AddBookToMemberCollection>(), "Add not consumed");

            Assert.IsTrue(await harness.Published.Any<Fault<AddBookToMemberCollection>>(), "Fault not published");

            Assert.IsTrue(await harness.Consumed.Any<Fault<AddBookToMemberCollection>>(), "Fault not consumed");
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