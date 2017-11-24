using System;
using System.Reactive.Linq;
using Toggl.Multivac.ReportsModels;
using Toggl.Ultrawave.ApiClients;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Serialization;
using Toggl.Ultrawave.ReportsModels;

namespace Toggl.Ultrawave.ReportsApiClients
{
    internal sealed class ProjectsSummaryApi : BaseApi, IProjectsSummaryApi
    {
        private readonly ProjectsSummaryEndpoints endPoints;
        private readonly IJsonSerializer serializer;
        private readonly Credentials credentials;

        public ProjectsSummaryApi(ProjectsSummaryEndpoints endPoints, IApiClient apiClient, IJsonSerializer serializer, Credentials credentials)
            : base(apiClient, serializer, credentials)
        {
            this.endPoints = endPoints;
            this.serializer = serializer;
            this.credentials = credentials;
        }

        public IObservable<IProjectsSummary> GetByWorkspace(long workspaceId, DateTimeOffset startDate, DateTimeOffset? endDate)
        {
            var parameters = new ProjectsSummaryParameters { StartDate = startDate, EndDate = endDate };
            var json = serializer.Serialize(parameters, SerializationReason.Post, null);
            return Observable.Create<IProjectsSummary>(async observer =>
            {
                var projectsSummaries = await CreateListObservable<ProjectSummary, IProjectSummary>(endPoints.Post(workspaceId), credentials.Header, json);
                var summary = new ProjectsSummary { StartDate = startDate, EndDate = endDate, ProjectsSummaries = projectsSummaries };

                observer.OnNext(summary);
                observer.OnCompleted();

                return () => { };
            });
        }

        private sealed class ProjectsSummaryParameters
        {
            public DateTimeOffset StartDate { get; set; }
            public DateTimeOffset? EndDate { get; set; }
        }
    }
}
