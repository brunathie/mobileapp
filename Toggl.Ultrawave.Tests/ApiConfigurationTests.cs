using System;
using FluentAssertions;
using Toggl.Ultrawave.Network;
using Xunit;
using static Toggl.Ultrawave.ApiEnvironment;
using static Toggl.Ultrawave.Network.Credentials;

namespace Toggl.Ultrawave.Tests
{
    public sealed class ApiConfigurationTests
    {
        public sealed class TheConstructor
        {
            private static readonly UserAgent correctUserAgent = new UserAgent("Test", "1.0");

            [Fact, LogIfTooSlow]
            public void ThrowsWhenTheCredentialsParameterIsNull()
            {
                Action constructingWithWrongParameteres =
                    () => new ApiConfiguration(Staging, null, correctUserAgent);

                constructingWithWrongParameteres
                    .ShouldThrow<ArgumentNullException>();
            }

            [Fact, LogIfTooSlow]
            public void ThrowsWhenTheUserAgentIsNull()
            {
                Action constructingWithWrongParameteres =
                    () => new ApiConfiguration(Staging, None, null);

                constructingWithWrongParameteres
                    .ShouldThrow<ArgumentException>();
            }
        }
    }
}
