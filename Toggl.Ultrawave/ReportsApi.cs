using Toggl.Multivac;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.ReportsApiClients;
using Toggl.Ultrawave.Serialization;

namespace Toggl.Ultrawave
{
    internal sealed class ReportsApi : IReportsApi
    {
        public ReportsApi(IApiClient apiClient, IJsonSerializer serializer, Endpoints endPoints, Credentials credentials)
        {
            Ensure.Argument.IsNotNull(apiClient, nameof(apiClient));
            Ensure.Argument.IsNotNull(serializer, nameof(serializer));
            Ensure.Argument.IsNotNull(endPoints, nameof(endPoints));
            Ensure.Argument.IsNotNull(credentials, nameof(credentials));

            ProjectsSummary = new ProjectsSummaryApi(endPoints, apiClient, serializer, credentials);
        }

        public IProjectsSummaryApi ProjectsSummary { get; }
    }
}
