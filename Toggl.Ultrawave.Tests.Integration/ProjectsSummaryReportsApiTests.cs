using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.ApiClients;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Helpers;
using Toggl.Ultrawave.Models;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Tests.Integration.BaseTests;
using Toggl.Ultrawave.Tests.Integration.Helper;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Toggl.Ultrawave.Tests.Integration
{
    public sealed class ProjectsSummaryReportsApiTests
    {
        public sealed class TheGetByWorkspaceMethod : EndpointTestBase
        {
            [Fact, LogIfTooSlow]
            public void ThrowsUnauthorizedExceptionWhenTheCredentialsAreInvalid()
            {
                var credentials = Credentials.WithPassword($"{Guid.NewGuid().ToString()}@mocks.toggl.com".ToEmail(), "123456");
                var api = TogglApiWith(credentials);

                Action unauthorizedCall = () => api.ReportsApi.ProjectsSummary.GetByWorkspace(123, DateTimeOffset.Now, null).Wait();

                unauthorizedCall.ShouldThrow<UnauthorizedException>();
            }

            [Fact, LogIfTooSlow]
            public async Task ThrowsForbiddenExceptionWhenTheUserDoesNotHaveAccessToTheWorkspace()
            {
                var (api, user) = await SetupTestUser();
                var inaccessibleWorkspaceId = user.DefaultWorkspaceId - 1;

                Action unauthorizedCall = () => api.ReportsApi.ProjectsSummary.GetByWorkspace(inaccessibleWorkspaceId, DateTimeOffset.Now, null).Wait();

                unauthorizedCall.ShouldThrow<ForbiddenException>();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsEmptyListWhenTheUserHasNoTimeEntriesInTheSpecifiedRange()
            {
                var (api, user) = await SetupTestUser();
                var start = DateTimeOffset.UtcNow.AddDays(-8);
                var end = start.AddDays(5);

                var summary = await api.ReportsApi.ProjectsSummary.GetByWorkspace(user.DefaultWorkspaceId, start, end);

                summary.ProjectsSummaries.Should().BeEmpty();
            }

            [Fact, LogIfTooSlow]
            public async Task UsesStartPlusSevenDaysWhenThereIsNoEndDateSpecified()
            {
                var (api, user) = await SetupTestUser();
                var start = DateTimeOffset.UtcNow.AddDays(-8);
                await createTimeEntry(api.TimeEntries, user, start.AddDays(2), 1);
                await createTimeEntry(api.TimeEntries, user, start.AddDays(8), 1);

                var summary =
                    await api.ReportsApi.ProjectsSummary.GetByWorkspace(user.DefaultWorkspaceId, start, null);

                summary.ProjectsSummaries.Should().HaveCount(1);
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotReturnTrackedBillableSecondsWhenWorkspaceIsNotPaid()
            {
                var (api, user) = await SetupTestUser();
                var start = DateTimeOffset.UtcNow.AddDays(-8);
                await createTimeEntry(api.TimeEntries, user, start.AddDays(2), 10, false);
                await createTimeEntry(api.TimeEntries, user, start.AddDays(8), 20, false);

                var summary = await api.ReportsApi.ProjectsSummary.GetByWorkspace(user.DefaultWorkspaceId, start, DateTimeOffset.UtcNow);

                summary.ProjectsSummaries.ForEach(project => project.BilledSeconds.Should().BeNull());
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTheSumOfTrackedSecondsForTheGivenInterval()
            {
                var (api, user) = await SetupTestUser();
                var start = DateTimeOffset.UtcNow.AddDays(-8);
                var project = await api.Projects.Create(new Project { Active = true, Name = Guid.NewGuid().ToString(), WorkspaceId = user.DefaultWorkspaceId });
                await createTimeEntry(api.TimeEntries, user, start.AddDays(2), 10, false, project.Id);
                await createTimeEntry(api.TimeEntries, user, start.AddDays(3), 20, false, project.Id);
                await createTimeEntry(api.TimeEntries, user, start.AddDays(8), 40, false, project.Id);

                var summary = await api.ReportsApi.ProjectsSummary.GetByWorkspace(user.DefaultWorkspaceId, start, start.AddDays(4));
                var projectSummary = summary.ProjectsSummaries.Single(s => s.ProjectId == project.Id);

                projectSummary.TrackedSeconds.Should().Be(30);
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTheSumOfTrackedSecondsForTheGivenIntervalForEachProject()
            {
                var (api, user) = await SetupTestUser();
                var start = DateTimeOffset.UtcNow.AddDays(-6);
                var projectA = await api.Projects.Create(new Project { Active = true, Name = Guid.NewGuid().ToString(), WorkspaceId = user.DefaultWorkspaceId });
                var projectB = await api.Projects.Create(new Project { Active = true, Name = Guid.NewGuid().ToString(), WorkspaceId = user.DefaultWorkspaceId });
                await createTimeEntry(api.TimeEntries, user, start.AddDays(2), 10, false, projectA.Id);
                await createTimeEntry(api.TimeEntries, user, start.AddDays(3), 20, false, projectA.Id);
                await createTimeEntry(api.TimeEntries, user, start.AddDays(4), 40, false, projectB.Id);
                await createTimeEntry(api.TimeEntries, user, start.AddDays(5), 25, false, projectB.Id);

                var summary =
                    await api.ReportsApi.ProjectsSummary.GetByWorkspace(user.DefaultWorkspaceId, start, null);
                var projectASummary = summary.ProjectsSummaries.Single(s => s.ProjectId == projectA.Id);
                var projectBSummary = summary.ProjectsSummaries.Single(s => s.ProjectId == projectB.Id);

                projectASummary.TrackedSeconds.Should().Be(30);
                projectBSummary.TrackedSeconds.Should().Be(65);
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTheSumOfBillableSecondsForTheGivenIntervalWhenTheWorkspaceIsPaid()
            {
                var (api, user) = await SetupTestUser();
                var start = DateTimeOffset.UtcNow.AddDays(-8);
                await WorkspaceHelper.SetSubscription(user, user.DefaultWorkspaceId, PricingPlans.StarterAnnual);
                var project = await api.Projects.Create(new Project { Active = true, Name = Guid.NewGuid().ToString(), WorkspaceId = user.DefaultWorkspaceId });
                await createTimeEntry(api.TimeEntries, user, start.AddDays(2), 10, false, project.Id);
                await createTimeEntry(api.TimeEntries, user, start.AddDays(3), 20, true, project.Id);
                await createTimeEntry(api.TimeEntries, user, start.AddDays(8), 40, true, project.Id);

                var summary = await api.ReportsApi.ProjectsSummary.GetByWorkspace(user.DefaultWorkspaceId, start, start.AddDays(5));
                var projectSummary = summary.ProjectsSummaries.Single(s => s.ProjectId == project.Id);

                projectSummary.TrackedSeconds.Should().Be(30);
                projectSummary.BilledSeconds.Should().Be(20);
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotIncludeRunningTimeEntriesInTheReports()
            {
                var (api, user) = await SetupTestUser();
                var start = DateTimeOffset.UtcNow.AddDays(-8);
                var project = await api.Projects.Create(new Project { Active = true, Name = Guid.NewGuid().ToString(), WorkspaceId = user.DefaultWorkspaceId });
                await createTimeEntry(api.TimeEntries, user, start.AddDays(1), null, false, project.Id);
                await createTimeEntry(api.TimeEntries, user, start.AddDays(2), 10, false, project.Id);
                await createTimeEntry(api.TimeEntries, user, start.AddDays(3), 20, false, project.Id);

                var summary = await api.ReportsApi.ProjectsSummary.GetByWorkspace(user.DefaultWorkspaceId, start, start.AddDays(4));
                var projectSummary = summary.ProjectsSummaries.Single(s => s.ProjectId == project.Id);

                projectSummary.TrackedSeconds.Should().Be(30);
            }

            [Fact, LogIfTooSlow]
            public async Task IncludesSummariesForArchivedProjects()
            {
                var (api, user) = await SetupTestUser();
                var start = DateTimeOffset.UtcNow.AddDays(-8);
                var project = await api.Projects.Create(new Project { Active = false, Name = Guid.NewGuid().ToString(), WorkspaceId = user.DefaultWorkspaceId });
                await createTimeEntry(api.TimeEntries, user, start.AddDays(2), 10, false, project.Id);

                var summary = await api.ReportsApi.ProjectsSummary.GetByWorkspace(user.DefaultWorkspaceId, start, start.AddDays(4));
                var projectSummary = summary.ProjectsSummaries.Single(s => s.ProjectId == project.Id);

                projectSummary.TrackedSeconds.Should().Be(10);
            }

            private async Task<ITimeEntry> createTimeEntry(ITimeEntriesApi api, IUser user, DateTimeOffset at, long? duration, bool billable = false, long? projectId = null)
                => await api.Create(new TimeEntry
                {
                    At = at,
                    Description = Guid.NewGuid().ToString(),
                    Start = at,
                    Duration = duration,
                    UserId = user.Id,
                    WorkspaceId = user.DefaultWorkspaceId,
                    TagIds = new long[0],
                    Billable = billable,
                    ProjectId = projectId
                });
        }
    }
}
