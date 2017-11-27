using System;
using System.Reactive.Linq;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States.Push
{
    public sealed class ResetAPIDelayStateTests
    {
        public sealed class TheStartMethod
        {
            [Fact, LogIfTooSlow]
            public void ReturnsThePushNextTransition()
            {
                var state = new ResetAPIDelayState(Substitute.For<IRetryDelayService>());

                var transition = state.Start().Wait();

                transition.Result.Should().Be(state.PushNext);
            }

            [Fact, LogIfTooSlow]
            public void ResetsTheRetryDelayServiceAfterProcessingThisState()
            {
                var delay = Substitute.For<IRetryDelayService>();
                var state = new ResetAPIDelayState(delay);

                state.Start().Wait();

                delay.Received().Reset();
            }
        }
    }
}
