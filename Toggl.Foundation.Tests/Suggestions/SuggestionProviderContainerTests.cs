using System;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Suggestions;
using Xunit;

namespace Toggl.Foundation.Tests.Suggestions
{
    public sealed class SuggestionProviderContainerTests
    {
        public sealed class TheConstructor
        {
            [Fact, LogIfTooSlow]
            public void ThrowsIfArgumentIsNull()
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new SuggestionProviderContainer(null);

                tryingToConstructWithEmptyParameters
                    .ShouldThrow<ArgumentNullException>();
            }

            [Fact, LogIfTooSlow]
            public void ThrowsIfNoArgumentsArePassed()
            {
                Action tryingToConstructWithoutParameters =
                    () => new SuggestionProviderContainer();

                tryingToConstructWithoutParameters
                    .ShouldThrow<ArgumentException>();
            }

            [Fact, LogIfTooSlow]
            public void WorksIfAtLeastOneProviderIsPassed()
            {
                var provider = Substitute.For<ISuggestionProvider>();
                Action tryingToConstructWithOneProvider =
                    () => new SuggestionProviderContainer(provider);

                tryingToConstructWithOneProvider
                    .ShouldNotThrow();
            }
        }
    }
}
