using System;
namespace Toggl.Ultrawave.Network
{
    internal sealed class ProjectsSummaryEndpoints
    {
        private readonly Uri baseUrl;

        public ProjectsSummaryEndpoints(Uri baseUrl)
        {
            this.baseUrl = baseUrl;
        }

        public Endpoint Post(long workspaceId)
            => Endpoint.Post(baseUrl, $"workspace/{workspaceId}/projects/summary");
    }
}
