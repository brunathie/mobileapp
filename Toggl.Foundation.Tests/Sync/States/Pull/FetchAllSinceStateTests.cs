using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using FluentAssertions;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.Ultrawave;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States
{
    public sealed class FetchAllSinceStateTests
    {
        public sealed class TheStartMethod
        {
            private readonly ITogglDatabase database;
            private readonly ITogglApi api;
            private readonly ITimeService timeService;
            private readonly FetchAllSinceState state;
            private readonly DateTimeOffset now = new DateTimeOffset(2017, 02, 15, 13, 50, 00, TimeSpan.Zero);

            public TheStartMethod()
            {
                database = Substitute.For<ITogglDatabase>();
                api = Substitute.For<ITogglApi>();
                timeService = Substitute.For<ITimeService>();
                timeService.CurrentDateTime.Returns(now);
                state = new FetchAllSinceState(database, api, timeService);
            }

            [Fact, LogIfTooSlow]
            public void EmitsTransitionToFetchStartedResult()
            {
                var transition = state.Start().SingleAsync().Wait();

                transition.Result.Should().Be(state.FetchStarted);
            }
            
            [Fact, LogIfTooSlow]
            public void MakesNoApiCallsBeforeSubscription()
            {
                state.Start();

                var w = api.DidNotReceive().Workspaces;
                var c = api.DidNotReceive().Clients;
                var p = api.DidNotReceive().Projects;
                var t = api.DidNotReceive().TimeEntries;
                var ta = api.DidNotReceive().Tags;
                var ts = api.DidNotReceive().Tasks;
                var wf = api.DidNotReceive().WorkspaceFeatures;
            }

            [Property]
            public void MakesCorrectCallsWithSinceThresholdsWhenSinceIsLessThanTwoMonthsInThePast(int seed)
            {
                var rnd = new Random(seed);
                var percent = rnd.NextDouble();

                var now = timeService.CurrentDateTime;
                var twoMonths = (now.AddMonths(2) - now);
                var seconds = twoMonths.TotalSeconds * percent;
                var since = now.AddSeconds(-seconds);

                var sinceParameters = Substitute.For<ISinceParameters>();
                sinceParameters.Clients.Returns(since);
                sinceParameters.Projects.Returns(since);
                sinceParameters.Tasks.Returns(since);
                sinceParameters.Tags.Returns(since);
                sinceParameters.Workspaces.Returns(since);
                sinceParameters.TimeEntries.Returns(since);

                database.SinceParameters.Returns(Substitute.For<ISinceParameterRepository>());
                database.SinceParameters.Get().Returns(sinceParameters);

                state.Start().Wait();
                
                api.Workspaces.Received().GetAll();
                api.Clients.Received().GetAllSince(since);
                api.Projects.Received().GetAllSince(since);
                api.TimeEntries.Received().GetAllSince(since);
                api.Tasks.Received().GetAllSince(since);
                api.Tags.Received().GetAllSince(since);
            }

            [Fact, LogIfTooSlow]
            public void MakesApiCallsWithoutTheSinceParameterWhenTheThresholdIsMoreThanTwoMonthsInThePast()
            {
                var now = timeService.CurrentDateTime;
                var sinceParameters = Substitute.For<ISinceParameters>();
                sinceParameters.Clients.Returns(now.AddMonths(-3));
                sinceParameters.Projects.Returns(now.AddMonths(-4));
                sinceParameters.Tasks.Returns(now.AddMonths(-5));
                sinceParameters.Tags.Returns(now.AddMonths(-6));
                sinceParameters.Workspaces.Returns(now.AddMonths(-7));
                sinceParameters.TimeEntries.Returns(now.AddMonths(-8));

                database.SinceParameters.Returns(Substitute.For<ISinceParameterRepository>());
                database.SinceParameters.Get().Returns(sinceParameters);

                state.Start().Wait();
                
                api.Workspaces.Received().GetAll();
                api.Clients.Received().GetAll();
                api.Projects.Received().GetAll();
                api.TimeEntries.Received().GetAll();
                api.Tasks.Received().GetAll();
                api.Tags.Received().GetAll();
            }

            [Fact, LogIfTooSlow]
            public void MakesCorrectCallsWithoutSinceThresholds()
            {
                state.Start().Wait();

                api.Workspaces.Received().GetAll();
                api.Clients.Received().GetAll();
                api.Projects.Received().GetAll();
                api.TimeEntries.Received().GetAll();
                api.Tasks.Received().GetAll();
                api.Tags.Received().GetAll();
            }

            [Fact, LogIfTooSlow]
            public void ConnectsToApiCallObservables()
            {
                var workspaceCall = false;
                var clientCall = false;
                var projectCall = false;
                var timeEntriesCall = false;
                var taskCall = false;
                var tagCall = false;

                api.Workspaces.GetAll().Returns(Observable.Create<List<IWorkspace>>(o => { workspaceCall = true; return () => { }; }));
                api.Clients.GetAll().Returns(Observable.Create<List<IClient>>(o => { clientCall = true; return () => { }; }));
                api.Projects.GetAll().Returns(Observable.Create<List<IProject>>(o => { projectCall = true; return () => { }; }));
                api.TimeEntries.GetAll().Returns(Observable.Create<List<ITimeEntry>>(o => { timeEntriesCall = true; return () => { }; }));
                api.Tasks.GetAll().Returns(Observable.Create<List<ITask>>(o => { taskCall = true; return () => { }; }));
                api.Tags.GetAll().Returns(Observable.Create<List<ITag>>(o => { tagCall = true; return () => { }; }));

                state.Start().Wait();

                workspaceCall.Should().BeTrue();
                clientCall.Should().BeTrue();
                projectCall.Should().BeTrue();
                timeEntriesCall.Should().BeTrue();
                taskCall.Should().BeTrue();
                tagCall.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsReplayingApiCallObservables()
            {
                api.Workspaces.GetAll().Returns(Observable.Return<List<IWorkspace>>(null));
                api.Clients.GetAll().Returns(Observable.Return<List<IClient>>(null));
                api.Projects.GetAll().Returns(Observable.Return<List<IProject>>(null));
                api.TimeEntries.GetAll().Returns(Observable.Return<List<ITimeEntry>>(null));
                api.Tasks.GetAll().Returns(Observable.Return<List<ITask>>(null));
                api.Tags.GetAll().Returns(Observable.Return<List<ITag>>(null));

                var transition = (Transition<FetchObservables>)state.Start().SingleAsync().Wait();

                var observables = transition.Parameter;
                observables.Workspaces.SingleAsync().Wait().Should().BeNull();
                observables.Clients.SingleAsync().Wait().Should().BeNull();
                observables.Projects.SingleAsync().Wait().Should().BeNull();
                observables.TimeEntries.SingleAsync().Wait().Should().BeNull();
                observables.Tasks.SingleAsync().Wait().Should().BeNull();
                observables.Tags.SingleAsync().Wait().Should().BeNull();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsSinceParametersFromDatabase()
            {
                var transition = (Transition<FetchObservables>)state.Start().SingleAsync().Wait();

                transition.Parameter.SinceParameters.ShouldBeEquivalentTo(database.SinceParameters.Get(), options => options.IncludingProperties());
            }
        }
    }
}
