using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Ultrawave.Helpers;
using Toggl.Ultrawave.Network;

namespace Toggl.Ultrawave.ApiClients
{
    internal sealed class StatusApi : IStatusApi
    {
        private readonly StatusEndpoints endpoints;
        private readonly IApiClient apiClient;

        public StatusApi(StatusEndpoints endpoints, IApiClient apiClient)
        {
            this.endpoints = endpoints;
            this.apiClient = apiClient;
        }

        public IObservable<Unit> IsAvailable()
        {
            return Observable.Create<Unit>(async observer =>
            {
                try
                {
                    var endpoint = endpoints.Get;
                    var request = new Request("", endpoint.Url, Enumerable.Empty<HttpHeader>(), endpoint.Method);
                    var response = await apiClient.Send(request).ConfigureAwait(false);

                    if (response.IsSuccess)
                    {
                        observer.OnNext(Unit.Default);
                    }
                    else
                    {
                        var error = ApiExceptions.For(request, response);
                        observer.OnError(error);
                    }
                }
                catch (Exception exception)
                {
                    observer.OnError(exception);
                }

                observer.OnCompleted();
            });
        }
    }
}
