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
    using StateMachines;


    public class When_a_reservation_is_added :
        StateMachineTestFixture<ReservationStateMachine, Reservation>
    {
        [Test]
        public async Task Should_create_a_saga_instance()
        {
            var reservationId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await TestHarness.Bus.Publish<ReservationRequested>(new
            {
                ReservationId = reservationId,
                InVar.Timestamp,
                MemberId = memberId,
                BookId = bookId,
            });

            Assert.IsTrue(await TestHarness.Consumed.Any<ReservationRequested>(), "Message not consumed");

            Assert.IsTrue(await SagaHarness.Consumed.Any<ReservationRequested>(), "Message not consumed by saga");

            Assert.That(await SagaHarness.Created.Any(x => x.CorrelationId == reservationId));

            var instance = SagaHarness.Created.ContainsInState(reservationId, Machine, Machine.Requested);
            Assert.IsNotNull(instance, "Saga instance not found");

            var existsId = await SagaHarness.Exists(reservationId, x => x.Requested);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");
        }
    }


    public class When_a_book_reservation_is_requested_for_an_available_book :
        StateMachineTestFixture<ReservationStateMachine, Reservation>
    {
        [Test]
        public async Task Should_reserve_the_book()
        {
            var reservationId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await TestHarness.Bus.Publish<BookAdded>(new
            {
                BookId = bookId,
                Isbn = "0307969959",
                Title = "Neuromancer"
            });

            var existsId = await _bookSagaHarness.Exists(bookId, x => x.Available);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            await TestHarness.Bus.Publish<ReservationRequested>(new
            {
                ReservationId = reservationId,
                InVar.Timestamp,
                MemberId = memberId,
                BookId = bookId,
            });

            Assert.IsTrue(await SagaHarness.Consumed.Any<ReservationRequested>(), "Message not consumed by saga");

            Assert.IsTrue(await _bookSagaHarness.Consumed.Any<ReservationRequested>(), "Message not consumed by saga");

            existsId = await SagaHarness.Exists(reservationId, x => x.Reserved);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            var reservation = SagaHarness.Sagas.ContainsInState(reservationId, Machine, x => x.Reserved);
            Assert.IsNotNull(reservation, "Reservation did not exist");
        }

        [OneTimeSetUp]
        public void TestSetup()
        {
            _bookSagaHarness = Provider.GetRequiredService<IStateMachineSagaTestHarness<Book, BookStateMachine>>();
            _bookMachine = Provider.GetRequiredService<BookStateMachine>();
        }

        IStateMachineSagaTestHarness<Book, BookStateMachine> _bookSagaHarness;
        BookStateMachine _bookMachine;

        protected override void ConfigureMassTransit(IServiceCollectionBusConfigurator configurator)
        {
            configurator.AddSagaStateMachine<BookStateMachine, Book>()
                .InMemoryRepository();

            configurator.AddPublishMessageScheduler();

            configurator.AddSagaStateMachineTestHarness<BookStateMachine, Book>();
        }
    }


    public class When_a_reservation_expires :
        StateMachineTestFixture<ReservationStateMachine, Reservation>
    {
        [Test]
        public async Task Should_mark_book_as_available()
        {
            var reservationId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await TestHarness.Bus.Publish<BookAdded>(new
            {
                BookId = bookId,
                Isbn = "0307969959",
                Title = "Neuromancer"
            });

            var existsId = await _bookSagaHarness.Exists(bookId, x => x.Available);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            await TestHarness.Bus.Publish<ReservationRequested>(new
            {
                ReservationId = reservationId,
                InVar.Timestamp,
                MemberId = memberId,
                BookId = bookId,
            });

            existsId = await SagaHarness.Exists(reservationId, x => x.Reserved);
            Assert.IsTrue(existsId.HasValue, "Reservation was not reserved");

            existsId = await _bookSagaHarness.Exists(bookId, x => x.Reserved);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            await AdvanceSystemTime(TimeSpan.FromHours(24));

            Guid? notExists = await SagaHarness.NotExists(reservationId);
            Assert.IsFalse(notExists.HasValue);

            existsId = await _bookSagaHarness.Exists(bookId, x => x.Available);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");
        }

        [OneTimeSetUp]
        public void TestSetup()
        {
            _bookSagaHarness = Provider.GetRequiredService<IStateMachineSagaTestHarness<Book, BookStateMachine>>();
            _bookMachine = Provider.GetRequiredService<BookStateMachine>();
        }

        IStateMachineSagaTestHarness<Book, BookStateMachine> _bookSagaHarness;
        BookStateMachine _bookMachine;

        protected override void ConfigureMassTransit(IServiceCollectionBusConfigurator configurator)
        {
            configurator.AddSagaStateMachine<BookStateMachine, Book>()
                .InMemoryRepository();

            configurator.AddPublishMessageScheduler();

            configurator.AddSagaStateMachineTestHarness<BookStateMachine, Book>();
        }
    }
}