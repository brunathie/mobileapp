using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FluentAssertions;
using FsCheck.Xunit;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States.Push;
using Toggl.Ultrawave;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States.Push
{
    public sealed class CheckServerStatusStateTests
    {
        private ITogglApi api;
        private TestScheduler scheduler;
        private IRetryDelayService apiDelay;
        private IRetryDelayService statusDelay;
        private readonly CheckServerStatusState state;
        private ISubject<Unit> delayCancellation;

        public CheckServerStatusStateTests()
        {
            api = Substitute.For<ITogglApi>();
            scheduler = new TestScheduler();
            apiDelay = Substitute.For<IRetryDelayService>();
            statusDelay = Substitute.For<IRetryDelayService>();
            delayCancellation = new Subject<Unit>();
            state = new CheckServerStatusState(api, scheduler, apiDelay, statusDelay, delayCancellation.AsObservable());
        }

        [Fact, LogIfTooSlow]
        public void ReturnsTheServerIsAvailableTransitionWhenTheStatusEndpointReturnsOK()
        {
            api.Status.IsAvailable().Returns(Observable.Return(Unit.Default));
            apiDelay.NextSlowDelay().Returns(TimeSpan.FromSeconds(1));

            ITransition transition = null;
            state.Start().Subscribe(t => transition = t);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            transition.Result.Should().Be(state.ServerIsAvailable);
        }

        [Fact, LogIfTooSlow]
        public void DoesNotResetTheStatusDelayServiceWhenTheStatusEndpointReturnsOKBeforeTheApiSlowDelay()
        {
            api.Status.IsAvailable().Returns(Observable.Return(Unit.Default));
            apiDelay.NextSlowDelay().Returns(TimeSpan.FromSeconds(1));

            state.Start().Subscribe(_ => { });
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks - 1);

            statusDelay.DidNotReceive().Reset();
        }

        [Fact, LogIfTooSlow]
        public void ResetsTheStatusDelayServiceWhenTheStatusEndpointReturnsOKAfterTheApiSlowDelay()
        {
            api.Status.IsAvailable().Returns(Observable.Return(Unit.Default));
            apiDelay.NextSlowDelay().Returns(TimeSpan.FromSeconds(1));

            state.Start().Subscribe(_ => { });
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            statusDelay.Received().Reset();
        }

        [Fact, LogIfTooSlow]
        public void DelaysTheTransitionByAtLeastTheSlowApiDelayTimeWhenTheStatusEndpointReturnsOK()
        {
            api.Status.IsAvailable().Returns(Observable.Return(Unit.Default));
            apiDelay.NextFastDelay().Returns(TimeSpan.FromSeconds(1));
            apiDelay.NextSlowDelay().Returns(TimeSpan.FromSeconds(10));
            statusDelay.NextFastDelay().Returns(TimeSpan.FromSeconds(1));
            statusDelay.NextSlowDelay().Returns(TimeSpan.FromSeconds(1));
            var hasCompleted = false;

            var transition = state.Start();
            var subscription = transition.Subscribe(_ => hasCompleted = true);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(10).Ticks - 1);
            subscription.Dispose();

            hasCompleted.Should().BeFalse();
        }

        [Fact, LogIfTooSlow]
        public void DelaysTheTransitionByAtMostTheSlowApiDelayTimeWhenTheStatusEndpointReturnsOK()
        {
            api.Status.IsAvailable().Returns(Observable.Return(Unit.Default));
            apiDelay.NextFastDelay().Returns(TimeSpan.FromSeconds(100));
            apiDelay.NextSlowDelay().Returns(TimeSpan.FromSeconds(10));
            statusDelay.NextFastDelay().Returns(TimeSpan.FromSeconds(100));
            statusDelay.NextSlowDelay().Returns(TimeSpan.FromSeconds(100));
            var hasCompleted = false;

            var transition = state.Start();
            var subscription = transition.Subscribe(_ => hasCompleted = true);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(10).Ticks);
            subscription.Dispose();

            hasCompleted.Should().BeTrue();
        }

        [Fact, LogIfTooSlow]
        public void DelaysTheTransitionAtMostByTheNextSlowDelayTimeFromTheRetryDelayServiceWhenInternalServerErrorOccurs()
        {
            api.Status.IsAvailable().Returns(Observable.Throw<Unit>(new InternalServerErrorException(request, response)));
            apiDelay.NextFastDelay().Returns(TimeSpan.FromSeconds(100));
            apiDelay.NextSlowDelay().Returns(TimeSpan.FromSeconds(10));
            var hasCompleted = false;

            var transition = state.Start();
            var subscription = transition.Subscribe(_ => hasCompleted = true);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(10).Ticks);
            subscription.Dispose();

            hasCompleted.Should().BeTrue();
        }

        [Fact, LogIfTooSlow]
        public void DelaysTheTransitionAtLeastByTheNextSlowDelayTimeFromTheRetryDelayServiceWhenInternalServerErrorOccurs()
        {
            api.Status.IsAvailable().Returns(Observable.Throw<Unit>(new InternalServerErrorException(request, response)));
            statusDelay.NextFastDelay().Returns(TimeSpan.FromSeconds(1));
            statusDelay.NextSlowDelay().Returns(TimeSpan.FromSeconds(10));
            var hasCompleted = false;

            var transition = state.Start();
            var subscription = transition.Subscribe(_ => hasCompleted = true);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(10).Ticks - 1);
            subscription.Dispose();

            hasCompleted.Should().BeFalse();
        }

        [Theory, LogIfTooSlow]
        [MemberData(nameof(ServerExceptionsOtherThanInternalServerErrorException))]
        public void DelaysTheTransitionAtMostByTheNextFastDelayTimeFromTheRetryDelayServiceWhenAServerErrorOtherThanInternalServerErrorOccurs(ServerErrorException exception)
        {
            api.Status.IsAvailable().Returns(Observable.Throw<Unit>(exception));
            statusDelay.NextFastDelay().Returns(TimeSpan.FromSeconds(10));
            statusDelay.NextSlowDelay().Returns(TimeSpan.FromSeconds(100));
            var hasCompleted = false;

            var transition = state.Start();
            var subscription = transition.Subscribe(_ => hasCompleted = true);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(10).Ticks + 1);
            subscription.Dispose();

            hasCompleted.Should().BeTrue();
        }

        [Theory, LogIfTooSlow]
        [MemberData(nameof(ServerExceptionsOtherThanInternalServerErrorException))]
        public void DelaysTheTransitionAtLeastByTheNextFastDelayTimeFromTheRetryDelayServiceWhenAServerErrorOtherThanInternalServerErrorOccurs(ServerErrorException exception)
        {
            api.Status.IsAvailable().Returns(Observable.Throw<Unit>(exception));
            statusDelay.NextFastDelay().Returns(TimeSpan.FromSeconds(10));
            statusDelay.NextSlowDelay().Returns(TimeSpan.FromSeconds(1));
            var hasCompleted = false;

            var transition = state.Start();
            var subscription = transition.Subscribe(_ => hasCompleted = true);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(10).Ticks - 1);
            subscription.Dispose();

            hasCompleted.Should().BeFalse();
        }

        [Fact, LogIfTooSlow]
        public void CompletesEvenThoughTheDelayIsNotOverButTheCancellationObservableIsNotifiedOfNewValue()
        {
            api.Status.IsAvailable().Returns(Observable.Return(Unit.Default));
            apiDelay.NextSlowDelay().Returns(TimeSpan.FromSeconds(10));

            ITransition transition = null;
            state.Start().Subscribe(t => transition = t);
            scheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);
            delayCancellation.OnNext(Unit.Default);

            transition.Result.Should().Be(state.ServerIsAvailable);
        }

        public static object[] ServerExceptionsOtherThanInternalServerErrorException()
            => new[]
            {
                new object[] { new BadGatewayException(request, response) },
                new object[] { new GatewayTimeoutException(request, response) },
                new object[] { new HttpVersionNotSupportedException(request, response) },
                new object[] { new ServiceUnavailableException(request, response) }
            };

        private static IRequest request => Substitute.For<IRequest>();

        private static IResponse response => Substitute.For<IResponse>();
    }
}
