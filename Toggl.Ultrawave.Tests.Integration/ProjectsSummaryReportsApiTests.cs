using System;
using System.Reactive.Linq;
using Toggl.Multivac.ReportsModels;
using Toggl.Ultrawave.Tests.Integration.BaseTests;

namespace Toggl.Ultrawave.Tests.Integration
{
    public sealed class ProjectsSummaryReportsApiTests
    {
        public sealed class TheGetByWorkspaceMethod : AuthenticatedEndpointBaseTests<IProjectsSummary>
        {
            protected override IObservable<IProjectsSummary> CallEndpointWith(ITogglApi togglApi)
            {
                var from = DateTimeOffset.UtcNow.AddDays(-2);
                var to = DateTimeOffset.UtcNow.AddDays(-1);

                return ValidApi.User
                    .Get()
                    .SelectMany(user =>
                        togglApi.ReportsApi.ProjectsSummary.GetByWorkspace(user.DefaultWorkspaceId, from, to));
            }
        }
    }
}
