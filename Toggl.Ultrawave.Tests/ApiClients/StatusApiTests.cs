using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using FluentAssertions;
using NSubstitute;
using Toggl.Ultrawave.ApiClients;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Helpers;
using Toggl.Ultrawave.Network;
using Xunit;
using static System.Net.HttpStatusCode;

namespace Toggl.Ultrawave.Tests.Clients
{
    public sealed class StatusClientTests
    {
        public sealed class TheIsAvailableMethod
        {
            private readonly StatusApi statusApi;
            private readonly IApiClient apiClient = Substitute.For<IApiClient>();

            public TheIsAvailableMethod()
            {
                var endpoints = new StatusEndpoints(ApiUrls.ForEnvironment(ApiEnvironment.Staging));
                statusApi = new StatusApi(endpoints, apiClient);
            }

            [Fact, LogIfTooSlow]
            public void DoesNotHideThrownExceptions()
            {
                bool caughtException = false;
                #pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
                apiClient
                    .Send(Arg.Any<IRequest>())
                    .Returns(async x => throw new WebException());
                #pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

                statusApi.IsAvailable()
                    .Catch((WebException exception) =>
                    {
                        caughtException = true;
                        return Observable.Return(Unit.Default);
                    }).Wait();

                caughtException.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsASingleValueForWorkingApiCalls()
            {
                apiClient
                    .Send(Arg.Any<IRequest>())
                    .Returns(x => new Response("OK", true, "text/plain", new List<KeyValuePair<string, IEnumerable<string>>>(), OK));

                statusApi.IsAvailable().Wait();
            }

            [Fact, LogIfTooSlow]
            public void ThrowsApiExceptionExceptionWhenApiServerIsNotAvailable()
            {
                apiClient
                    .Send(Arg.Any<IRequest>())
                    .Returns(x => new Response("PANIC", false, "text/plain", new List<KeyValuePair<string, IEnumerable<string>>>(), InternalServerError));

                Action gettingServerStatus = () => statusApi.IsAvailable().Wait();

                gettingServerStatus.ShouldThrow<InternalServerErrorException>();
            }
        }
    }
}
