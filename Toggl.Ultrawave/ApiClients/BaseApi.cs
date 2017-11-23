using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.Helpers;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Serialization;
using Toggl.Ultrawave.Exceptions;

namespace Toggl.Ultrawave.ApiClients
{
    internal abstract class BaseApi
    {
        private readonly IApiClient apiClient;
        private readonly IJsonSerializer serializer;

        protected HttpHeader AuthHeader { get; }

        protected BaseApi(IApiClient apiClient, IJsonSerializer serializer, Credentials credentials)
        {
            Ensure.Argument.IsNotNull(apiClient, nameof(apiClient));
            Ensure.Argument.IsNotNull(serializer, nameof(serializer));
            Ensure.Argument.IsNotNull(credentials, nameof(credentials));

            this.apiClient = apiClient;
            this.serializer = serializer;
            this.AuthHeader = credentials.Header;
        }

        protected IObservable<List<TInterface>> CreateListObservable<TModel, TInterface>(Endpoint endpoint, HttpHeader header, List<TModel> entities, SerializationReason serializationReason, IWorkspaceFeatureCollection features = null)
            where TModel : class, TInterface
        {
            var body = serializer.Serialize(entities, serializationReason, features);
            return CreateListObservable<TModel, TInterface>(endpoint, header, body);
        }

        protected IObservable<List<TInterface>> CreateListObservable<TModel, TInterface>(Endpoint endpoint, HttpHeader header, string body = "")
            where TModel : class, TInterface
            => CreateListObservable<TModel, TInterface>(endpoint, new[] { header }, body);


        protected IObservable<List<TInterface>> CreateListObservable<TModel, TInterface>(Endpoint endpoint, IEnumerable<HttpHeader> headers, string body = "")
            where TModel : class, TInterface
        {
            var observable = CreateObservable<List<TModel>>(endpoint, headers, body);
            return observable.Select(items => items?.ToList<TInterface>());
        }

        protected IObservable<T> CreateObservable<T>(Endpoint endpoint, HttpHeader header, T entity, SerializationReason serializationReason, IWorkspaceFeatureCollection features = null) {
            var body = serializer.Serialize<T>(entity, serializationReason, features);
            return CreateObservable<T>(endpoint, header, body);
        }
        
        protected IObservable<T> CreateObservable<T>(Endpoint endpoint, HttpHeader header, string body = "")
            => CreateObservable<T>(endpoint, new[] { header }, body);

        protected IObservable<T> CreateObservable<T>(Endpoint endpoint, IEnumerable<HttpHeader> headers, string body = "")
            => createObservable(endpoint, headers, body, async (rawData) =>
                !string.IsNullOrEmpty(rawData)
                    ? await Task.Run(() => serializer.Deserialize<T>(rawData)).ConfigureAwait(false)
                    : default(T));

        protected IObservable<string> CreateObservable(Endpoint endpoint, IEnumerable<HttpHeader> headers, string body = "")
            => createObservable(endpoint, headers, body, (rawData) => Task.FromResult(rawData));

        private IObservable<T> createObservable<T>(Endpoint endpoint, IEnumerable<HttpHeader> headers, string body, Func<string, Task<T>> process)
        {
            var request = new Request(body, endpoint.Url, headers, endpoint.Method);
            return Observable.Create<T>(async observer =>
            {
                IResponse response;
                try
                {
                    response = await apiClient.Send(request).ConfigureAwait(false);
                }
                catch (HttpRequestException)
                {
                    observer.OnError(new OfflineException());
                    return;
                }

                if (!response.IsSuccess)
                {
                    var exception = ApiExceptions.For(request, response);
                    observer.OnError(exception);
                    return;
                }

                T data;
                try
                {
                    data = await process(response.RawData);
                }
                catch
                {
                    observer.OnError(new DeserializationException<T>(request, response, response.RawData));
                    return;
                }

                observer.OnNext(data);
                observer.OnCompleted();
            });
        }
    }
}
