namespace Library.Components.Tests
{
    using System;
    using System.Threading.Tasks;
    using Contracts;
    using MassTransit;
    using MassTransit.QuartzIntegration;
    using MassTransit.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using StateMachines;


    public class When_a_reservation_is_added
    {
        [Test]
        public async Task Should_create_a_saga_instance()
        {
            await using var provider = new ServiceCollection()
                .ConfigureMassTransit(x =>
                {
                    x.AddSagaStateMachine<ReservationStateMachine, Reservation>();
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var sagaHarness = harness.GetSagaStateMachineHarness<ReservationStateMachine, Reservation>();

            var reservationId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await harness.Bus.Publish<ReservationRequested>(new
            {
                ReservationId = reservationId,
                InVar.Timestamp,
                MemberId = memberId,
                BookId = bookId,
            });

            Assert.IsTrue(await harness.Consumed.Any<ReservationRequested>(), "Message not consumed");

            Assert.IsTrue(await sagaHarness.Consumed.Any<ReservationRequested>(), "Message not consumed by saga");

            Assert.That(await sagaHarness.Created.Any(x => x.CorrelationId == reservationId));

            var instance = sagaHarness.Created.ContainsInState(reservationId, sagaHarness.StateMachine, sagaHarness.StateMachine.Requested);
            Assert.IsNotNull(instance, "Saga instance not found");

            Guid? existsId = await sagaHarness.Exists(reservationId, x => x.Requested);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");
        }
    }


    public class When_a_book_reservation_is_requested_for_an_available_book
    {
        [Test]
        public async Task Should_reserve_the_book()
        {
            await using var provider = new ServiceCollection()
                .ConfigureMassTransit(x =>
                {
                    x.AddSagaStateMachine<ReservationStateMachine, Reservation>();
                    x.AddSagaStateMachine<BookStateMachine, Book>();
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var sagaHarness = harness.GetSagaStateMachineHarness<ReservationStateMachine, Reservation>();
            var bookSagaHarness = harness.GetSagaStateMachineHarness<BookStateMachine, Book>();

            var reservationId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await harness.Bus.Publish<BookAdded>(new
            {
                BookId = bookId,
                Isbn = "0307969959",
                Title = "Neuromancer"
            });

            Guid? existsId = await bookSagaHarness.Exists(bookId, x => x.Available);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            await harness.Bus.Publish<ReservationRequested>(new
            {
                ReservationId = reservationId,
                InVar.Timestamp,
                MemberId = memberId,
                BookId = bookId,
            });

            Assert.IsTrue(await sagaHarness.Consumed.Any<ReservationRequested>(), "Message not consumed by saga");

            Assert.IsTrue(await bookSagaHarness.Consumed.Any<ReservationRequested>(), "Message not consumed by saga");

            existsId = await sagaHarness.Exists(reservationId, x => x.Reserved);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            var reservation = sagaHarness.Sagas.ContainsInState(reservationId, sagaHarness.StateMachine, x => x.Reserved);
            Assert.IsNotNull(reservation, "Reservation did not exist");
        }
    }


    public class When_a_reservation_expires
    {
        [Test]
        public async Task Should_mark_book_as_available()
        {
            await using var provider = new ServiceCollection()
                .ConfigureMassTransit(x =>
                {
                    x.AddSagaStateMachine<ReservationStateMachine, Reservation>();
                    x.AddSagaStateMachine<BookStateMachine, Book>();
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var sagaHarness = harness.GetSagaStateMachineHarness<ReservationStateMachine, Reservation>();
            var bookSagaHarness = harness.GetSagaStateMachineHarness<BookStateMachine, Book>();

            var reservationId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await harness.Bus.Publish<BookAdded>(new
            {
                BookId = bookId,
                Isbn = "0307969959",
                Title = "Neuromancer"
            });

            Guid? existsId = await bookSagaHarness.Exists(bookId, x => x.Available);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            await harness.Bus.Publish<ReservationRequested>(new
            {
                ReservationId = reservationId,
                InVar.Timestamp,
                MemberId = memberId,
                BookId = bookId,
            });

            existsId = await sagaHarness.Exists(reservationId, x => x.Reserved);
            Assert.IsTrue(existsId.HasValue, "Reservation was not reserved");

            existsId = await bookSagaHarness.Exists(bookId, x => x.Reserved);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            using var adjustment = new QuartzTimeAdjustment(provider);

            await adjustment.AdvanceTime(TimeSpan.FromHours(24));

            Guid? notExists = await sagaHarness.NotExists(reservationId);
            Assert.IsFalse(notExists.HasValue);

            existsId = await bookSagaHarness.Exists(bookId, x => x.Available);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");
        }
    }


    public class When_a_reservation_expires_with_custom_duration
    {
        [Test]
        public async Task Should_mark_book_as_available()
        {
            await using var provider = new ServiceCollection()
                .ConfigureMassTransit(x =>
                {
                    x.AddSagaStateMachine<ReservationStateMachine, Reservation>();
                    x.AddSagaStateMachine<BookStateMachine, Book>();
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var sagaHarness = harness.GetSagaStateMachineHarness<ReservationStateMachine, Reservation>();
            var bookSagaHarness = harness.GetSagaStateMachineHarness<BookStateMachine, Book>();

            var reservationId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await harness.Bus.Publish<BookAdded>(new
            {
                BookId = bookId,
                Isbn = "0307969959",
                Title = "Neuromancer"
            });

            Guid? existsId = await bookSagaHarness.Exists(bookId, x => x.Available);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            await harness.Bus.Publish<ReservationRequested>(new
            {
                ReservationId = reservationId,
                InVar.Timestamp,
                Duration = TimeSpan.FromDays(2),
                MemberId = memberId,
                BookId = bookId,
            });

            existsId = await sagaHarness.Exists(reservationId, x => x.Reserved);
            Assert.IsTrue(existsId.HasValue, "Reservation was not reserved");

            existsId = await bookSagaHarness.Exists(bookId, x => x.Reserved);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            using var adjustment = new QuartzTimeAdjustment(provider);

            await adjustment.AdvanceTime(TimeSpan.FromHours(24));

            existsId = await sagaHarness.Exists(reservationId, x => x.Reserved);
            Assert.IsTrue(existsId.HasValue, "Reservation was not still reserved");

            await adjustment.AdvanceTime(TimeSpan.FromHours(24));

            Guid? notExists = await sagaHarness.NotExists(reservationId);
            Assert.IsFalse(notExists.HasValue);

            existsId = await bookSagaHarness.Exists(bookId, x => x.Available);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");
        }
    }


    public class When_a_reservation_is_canceled
    {
        [Test]
        public async Task Should_mark_book_as_available()
        {
            await using var provider = new ServiceCollection()
                .ConfigureMassTransit(x =>
                {
                    x.AddSagaStateMachine<ReservationStateMachine, Reservation>();
                    x.AddSagaStateMachine<BookStateMachine, Book>();
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var sagaHarness = harness.GetSagaStateMachineHarness<ReservationStateMachine, Reservation>();
            var bookSagaHarness = harness.GetSagaStateMachineHarness<BookStateMachine, Book>();

            var reservationId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await harness.Bus.Publish<BookAdded>(new
            {
                BookId = bookId,
                Isbn = "0307969959",
                Title = "Neuromancer"
            });

            Guid? existsId = await bookSagaHarness.Exists(bookId, x => x.Available);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            await harness.Bus.Publish<ReservationRequested>(new
            {
                ReservationId = reservationId,
                InVar.Timestamp,
                MemberId = memberId,
                BookId = bookId,
            });

            existsId = await sagaHarness.Exists(reservationId, x => x.Reserved);
            Assert.IsTrue(existsId.HasValue, "Reservation was not reserved");

            existsId = await bookSagaHarness.Exists(bookId, x => x.Reserved);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            await harness.Bus.Publish<ReservationCancellationRequested>(new
            {
                ReservationId = reservationId,
                InVar.Timestamp
            });

            Guid? notExists = await sagaHarness.NotExists(reservationId);
            Assert.IsFalse(notExists.HasValue);

            existsId = await bookSagaHarness.Exists(bookId, x => x.Available);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");
        }
    }


    public class When_a_reserved_book_is_checked_out
    {
        [Test]
        public async Task The_reservation_should_be_removed()
        {
            await using var provider = new ServiceCollection()
                .ConfigureMassTransit(x =>
                {
                    x.AddSagaStateMachine<ReservationStateMachine, Reservation>();
                    x.AddSagaStateMachine<BookStateMachine, Book>();
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var sagaHarness = harness.GetSagaStateMachineHarness<ReservationStateMachine, Reservation>();
            var bookSagaHarness = harness.GetSagaStateMachineHarness<BookStateMachine, Book>();

            var reservationId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await harness.Bus.Publish<BookAdded>(new
            {
                BookId = bookId,
                Isbn = "0307969959",
                Title = "Neuromancer"
            });

            Guid? existsId = await bookSagaHarness.Exists(bookId, x => x.Available);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            await harness.Bus.Publish<ReservationRequested>(new
            {
                ReservationId = reservationId,
                InVar.Timestamp,
                MemberId = memberId,
                BookId = bookId,
            });

            existsId = await sagaHarness.Exists(reservationId, x => x.Reserved);
            Assert.IsTrue(existsId.HasValue, "Reservation was not reserved");

            existsId = await bookSagaHarness.Exists(bookId, x => x.Reserved);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            await harness.Bus.Publish<BookCheckedOut>(new
            {
                BookId = bookId,
                InVar.Timestamp
            });

            Guid? notExists = await sagaHarness.NotExists(reservationId);
            Assert.IsFalse(notExists.HasValue);

            existsId = await bookSagaHarness.Exists(bookId, x => x.CheckedOut);
            Assert.IsTrue(existsId.HasValue, "Book was not checked out");
        }
    }


    public class When_a_reservation_for_an_already_reserved_book_is_requested
    {
        [Test]
        public async Task Should_not_reserve_the_book()
        {
            await using var provider = new ServiceCollection()
                .ConfigureMassTransit(x =>
                {
                    x.AddSagaStateMachine<ReservationStateMachine, Reservation>();
                    x.AddSagaStateMachine<BookStateMachine, Book>();
                })
                .BuildServiceProvider(true);

            var harness = provider.GetTestHarness();

            await harness.Start();

            var sagaHarness = harness.GetSagaStateMachineHarness<ReservationStateMachine, Reservation>();
            var bookSagaHarness = harness.GetSagaStateMachineHarness<BookStateMachine, Book>();

            var reservationId = NewId.NextGuid();
            var bookId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await harness.Bus.Publish<BookAdded>(new
            {
                BookId = bookId,
                Isbn = "0307969959",
                Title = "Neuromancer"
            });

            Guid? existsId = await bookSagaHarness.Exists(bookId, x => x.Available);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            await harness.Bus.Publish<ReservationRequested>(new
            {
                ReservationId = reservationId,
                InVar.Timestamp,
                MemberId = memberId,
                BookId = bookId,
            });

            existsId = await sagaHarness.Exists(reservationId, x => x.Reserved);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            var reservation = sagaHarness.Sagas.ContainsInState(reservationId, sagaHarness.StateMachine, x => x.Reserved);
            Assert.IsNotNull(reservation, "Reservation did not exist");

            var secondReservationId = NewId.NextGuid();

            await harness.Bus.Publish<ReservationRequested>(new
            {
                ReservationId = secondReservationId,
                InVar.Timestamp,
                MemberId = NewId.NextGuid(),
                BookId = bookId,
            });

            existsId = await sagaHarness.Exists(secondReservationId, x => x.Requested);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");

            var secondReservation = sagaHarness.Sagas.ContainsInState(secondReservationId, sagaHarness.StateMachine, x => x.Requested);
            Assert.IsNotNull(secondReservation, "Reservation did not exist");
        }
    }
}