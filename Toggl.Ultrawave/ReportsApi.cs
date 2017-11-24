using System.Net.Http;
using ModernHttpClient;
using Toggl.Multivac;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.ReportsApiClients;
using Toggl.Ultrawave.Serialization;
using static System.Net.DecompressionMethods;

namespace Toggl.Ultrawave
{
    public sealed class ReportsApi : IReportsApi
    {
        public ReportsApi(ApiConfiguration configuration, HttpClientHandler handler = null)
        {
            Ensure.Argument.IsNotNull(configuration, nameof(configuration));

            var httpHandler = handler ?? new NativeMessageHandler { AutomaticDecompression = GZip | Deflate };
            var httpClient = new HttpClient(httpHandler);

            var userAgent = configuration.UserAgent;
            var credentials = configuration.Credentials;
            var serializer = new JsonSerializer();
            var apiClient = new ApiClient(httpClient, userAgent);
            var endpoints = new ReportsEndpoints(configuration.Environment);

            ProjectsSummary = new ProjectsSummaryApi(endpoints.ProjectsSummary, apiClient, serializer, credentials);
        }

        public IProjectsSummaryApi ProjectsSummary { get; }
    }
}
