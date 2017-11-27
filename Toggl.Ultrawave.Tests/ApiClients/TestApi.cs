using System;
using System.Collections.Generic;
using Toggl.Multivac;
using Toggl.Ultrawave.ApiClients;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Serialization;

namespace Toggl.Ultrawave.Tests.ApiClients
{
    internal sealed class TestApi : BaseApi
    {
        private readonly Endpoint endpoint;

        public TestApi(Endpoint endpoint, IApiClient apiClient, IJsonSerializer serializer,
            Credentials credentials, Endpoint loggedEndpoint)
            : base(apiClient, serializer, credentials, loggedEndpoint)
        {
            Ensure.Argument.IsNotNull(endpoint, nameof(endpoint));

            this.endpoint = endpoint;
        }

        public IObservable<T> TestCreateObservable<T>(Endpoint endpoint, IEnumerable<HttpHeader> headers, string body = "")
            => CreateObservable<T>(endpoint, headers, body);

        public IObservable<string> Get()
        {
            var observable = CreateObservable<string>(endpoint, AuthHeader);
            return observable;
        }
    }
}
