using System;
using System.Linq;
using FluentAssertions;
using Toggl.Ultrawave.Helpers;
using Toggl.Ultrawave.Tests.Integration.BaseTests;
using Toggl.Ultrawave.Tests.Integration.Helper;
using Xunit;

namespace Toggl.Ultrawave.Tests.Integration
{
    public sealed class PricingPlansTests : EndpointTestBase
    {
        [Fact]
        public async void ThePricingPlansEnumsContainsAllAndOnlyTheAvailablePricingPlans()
        {
            var (_, user) = await SetupTestUser();

            var availablePlans = await WorkspaceHelper.GetAllAvailablePricingPlans(user);
            var convertedPlans = availablePlans.Select(plan => (PricingPlans)plan);

            Enum.GetNames(typeof(PricingPlans)).Length.Should().Be(availablePlans.Count);
            Enum.GetValues(typeof(PricingPlans)).Should().Contain(convertedPlans);
        }
    }
}
