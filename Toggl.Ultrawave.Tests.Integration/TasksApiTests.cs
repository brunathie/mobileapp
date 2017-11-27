using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reactive.Linq;
using FluentAssertions;
using Xunit;
using Toggl.Ultrawave.Models;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Tests.Integration.BaseTests;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.Helpers;
using Toggl.Ultrawave.Tests.Integration.Helper;

namespace Toggl.Ultrawave.Tests.Integration
{
    public sealed class TasksApiTests
    {
        public sealed class TheGetAllMethod : AuthenticatedEndpointBaseTests<List<ITask>>
        {
            private readonly SubscriptionPlanActivator plans = new SubscriptionPlanActivator();

            protected override IObservable<List<ITask>> CallEndpointWith(ITogglApi togglApi)
            {
                plans.EnsureDefaultWorkspaceIsOnPlan(togglApi, PricingPlans.StarterMonthly).Wait();
                return togglApi.Tasks.GetAll();
            }

            [Fact, LogTestInfo]
            public async System.Threading.Tasks.Task ReturnsAllTasks()
            {
                var (togglClient, user) = await SetupTestUser();
                var project = await createProject(togglClient, user.DefaultWorkspaceId);
                await plans.EnsureDefaultWorkspaceIsOnPlan(user, PricingPlans.StarterMonthly);
                var taskA = randomTask(project, user.Id);
                await togglClient.Tasks.Create(taskA);
                var taskB = randomTask(project, user.Id);
                await togglClient.Tasks.Create(taskB);

                var tasks = await togglClient.Tasks.GetAll();

                tasks.Should().HaveCount(2);
                tasks.Should().Contain(x => isTheSameAs(taskA, x));
                tasks.Should().Contain(x => isTheSameAs(taskB, x));
            }

            [Fact, LogTestInfo]
            public async System.Threading.Tasks.Task ReturnsOnlyActiveTasks()
            {
                var (togglClient, user) = await SetupTestUser();
                var project = await createProject(togglClient, user.DefaultWorkspaceId);
                await plans.EnsureDefaultWorkspaceIsOnPlan(user, PricingPlans.StarterMonthly);
                var task = randomTask(project, user.Id);
                await togglClient.Tasks.Create(task);
                var inactiveTask = randomTask(project, user.Id, isActive: false);
                await togglClient.Tasks.Create(inactiveTask);

                var tasks = await togglClient.Tasks.GetAll();

                tasks.Should().HaveCount(1);
                tasks.Should().Contain(x => isTheSameAs(x, task));
                tasks.Should().NotContain(x => isTheSameAs(x, inactiveTask));
            }

            [Fact, LogTestInfo]
            public async System.Threading.Tasks.Task ReturnsEmptyListWhenThereAreNoActiveTasks()
            {
                var (togglClient, user) = await SetupTestUser();
                var project = await createProject(togglClient, user.DefaultWorkspaceId);
                await plans.EnsureDefaultWorkspaceIsOnPlan(user, PricingPlans.StarterMonthly);
                await togglClient.Tasks.Create(randomTask(project, user.Id, isActive: false));

                var tasks = await togglClient.Tasks.GetAll();

                tasks.Should().HaveCount(0);
            }
        }

        public sealed class TheGetAllSinceMethod : AuthenticatedGetSinceEndpointBaseTests<ITask>
        {
            private readonly SubscriptionPlanActivator plans = new SubscriptionPlanActivator();

            private IProject project;

            protected override IObservable<List<ITask>> CallEndpointWith(ITogglApi togglApi, DateTimeOffset threshold)
            {
                plans.EnsureDefaultWorkspaceIsOnPlan(togglApi, PricingPlans.StarterMonthly).Wait();
                return togglApi.Tasks.GetAllSince(threshold);
            }

            protected override DateTimeOffset AtDateOf(ITask model)
                => model.At;

            protected override ITask MakeUniqueModel(ITogglApi api, IUser user)
                => new Task
                {
                    Active = true,
                    Name = Guid.NewGuid().ToString(),
                    WorkspaceId = user.DefaultWorkspaceId,
                    ProjectId = getProject(api, user.DefaultWorkspaceId).Id,
                    At = DateTimeOffset.UtcNow
                };

            protected override IObservable<ITask> PostModelToApi(ITogglApi api, ITask model)
            {
                plans.EnsureDefaultWorkspaceIsOnPlan(api, PricingPlans.StarterMonthly).Wait();
                return api.Tasks.Create(model);
            }

            protected override Expression<Func<ITask, bool>> ModelWithSameAttributesAs(ITask model)
                => t => isTheSameAs(model, t);

            private IProject getProject(ITogglApi api, long workspaceId)
                => project ?? (project = createProject(api, workspaceId).Wait());
        }

        public sealed class TheCreateMethod : AuthenticatedPostEndpointBaseTests<ITask>
        {
            private readonly SubscriptionPlanActivator plans = new SubscriptionPlanActivator();

            [Fact, LogIfTooSlow]
            public async void CreatingTaskFailsInTheFreePlan()
            {
                var (togglApi, user) = await SetupTestUser();
                var project = await createProject(togglApi, user.DefaultWorkspaceId);

                Action creatingTask = () => createTask(togglApi, project, user.Id).Wait();

                creatingTask.ShouldThrow<ForbiddenException>();
            }

            [Theory, LogIfTooSlow]
            [InlineData(PricingPlans.StarterMonthly)]
            [InlineData(PricingPlans.StarterAnnual)]
            [InlineData(PricingPlans.PremiumMonthly)]
            [InlineData(PricingPlans.PremiumAnnual)]
            [InlineData(PricingPlans.EnterpriseMonthly)]
            [InlineData(PricingPlans.EnterpriseAnnual)]
            public async void CreatingTaskWorksForAllPricingPlansOtherThanTheFreePlan(PricingPlans plan)
            {
                var (togglApi, user) = await SetupTestUser();
                await plans.EnsureDefaultWorkspaceIsOnPlan(user, plan);
                var project = createProject(togglApi, user.DefaultWorkspaceId).Wait();

                Action creatingTask = () => createTask(togglApi, project, user.Id).Wait();

                creatingTask.ShouldNotThrow();
            }

            protected override IObservable<ITask> CallEndpointWith(ITogglApi togglApi)
            {
                var user = togglApi.User.Get().Wait();
                plans.EnsureDefaultWorkspaceIsOnPlan(user, PricingPlans.StarterMonthly).Wait();
                var project = createProject(togglApi, user.DefaultWorkspaceId).Wait();
                return createTask(togglApi, project, user.Id);
            }
        }

        private static ITask randomTask(IProject project, long userId, bool isActive = true)
            => new Task
            {
                WorkspaceId = project.WorkspaceId,
                ProjectId = project.Id,
                UserId = userId,
                Name = Guid.NewGuid().ToString(),
                Active = isActive,
                At = DateTimeOffset.UtcNow
            };

        private static IProject randomProject(long workspaceId)
            => new Project
            {
                WorkspaceId = workspaceId,
                Name = Guid.NewGuid().ToString(),
                Active = true,
                At = DateTimeOffset.UtcNow
            };

        private static IObservable<IProject> createProject(ITogglApi togglApi, long workspaceId)
            => togglApi.Projects.Create(randomProject(workspaceId));

        private static IObservable<ITask> createTask(ITogglApi togglApi, IProject project, long userId)
            => togglApi.Tasks.Create(randomTask(project, userId));

        private static bool isTheSameAs(ITask a, ITask b)
            => a.Name == b.Name
               && a.Active == b.Active
               && a.ProjectId == b.ProjectId
               && a.EstimatedSeconds == b.EstimatedSeconds
               && a.TrackedSeconds == b.TrackedSeconds
               && a.WorkspaceId == b.WorkspaceId
               && a.UserId == b.UserId;
    }
}
