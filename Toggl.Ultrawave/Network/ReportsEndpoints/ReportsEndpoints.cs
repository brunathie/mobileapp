using System;
using Toggl.Ultrawave.Helpers;

namespace Toggl.Ultrawave.Network
{
    internal sealed class ReportsEndpoints
    {
        private readonly Uri baseUrl;

        public ProjectsSummaryEndpoints ProjectsSummary => new ProjectsSummaryEndpoints(baseUrl);

        public ReportsEndpoints(ApiEnvironment environment)
        {
            baseUrl = ReportsUrls.ForEnvironment(environment);
        }
    }
}
