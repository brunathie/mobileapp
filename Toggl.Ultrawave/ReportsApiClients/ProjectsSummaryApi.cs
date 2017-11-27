using System;
using System.Reactive.Linq;
using Newtonsoft.Json;
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

        public ProjectsSummaryApi(Endpoints endPoints, IApiClient apiClient, IJsonSerializer serializer, Credentials credentials)
            : base(apiClient, serializer, credentials, endPoints.LoggedIn)
        {
            this.endPoints = endPoints.ReportsEndpoints.ProjectsSummary;
            this.serializer = serializer;
            this.credentials = credentials;
        }

        public IObservable<IProjectsSummary> GetByWorkspace(long workspaceId, DateTimeOffset startDate, DateTimeOffset? endDate)
        {
            var interval = endDate - startDate;
            if (interval.HasValue && interval > TimeSpan.FromDays(365))
                throw new ArgumentOutOfRangeException(nameof(endDate));

            var parameters = new ProjectsSummaryParameters(startDate, endDate);
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
            public string StartDate { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string EndDate { get; set; }

            public ProjectsSummaryParameters() { }

            public ProjectsSummaryParameters(DateTimeOffset startDate, DateTimeOffset? endDate)
            {
                StartDate = startDate.ToString("yyyy-MM-dd");
                EndDate = endDate?.ToString("yyyy-MM-dd");
            }
        }
    }
}
