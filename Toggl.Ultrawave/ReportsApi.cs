using Toggl.Multivac;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.ReportsApiClients;
using Toggl.Ultrawave.Serialization;

namespace Toggl.Ultrawave
{
    internal sealed class ReportsApi : IReportsApi
    {
        public ReportsApi(IApiClient apiClient, IJsonSerializer serializer, ApiEnvironment environment, Credentials credentials)
        {
            Ensure.Argument.IsNotNull(apiClient, nameof(apiClient));
            Ensure.Argument.IsNotNull(serializer, nameof(serializer));
            Ensure.Argument.IsNotNull(environment, nameof(environment));
            Ensure.Argument.IsNotNull(credentials, nameof(credentials));

            var endpoints = new ReportsEndpoints(environment);

            ProjectsSummary = new ProjectsSummaryApi(endpoints.ProjectsSummary, apiClient, serializer, credentials);
        }

        public IProjectsSummaryApi ProjectsSummary { get; }
    }
}
