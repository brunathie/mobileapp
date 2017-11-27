using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Suggestions;
using Toggl.Foundation.Tests.Generators;
using Toggl.PrimeRadiant.Models;
using Xunit;
using TimeEntry = Toggl.Foundation.Models.TimeEntry;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SuggestionsViewModelTests
    {
        public abstract class SuggestionsViewModelTest : BaseViewModelTests<SuggestionsViewModel>
        {
            protected TestScheduler Scheduler { get; } = new TestScheduler();

            protected ISuggestionProviderContainer Container { get; } = Substitute.For<ISuggestionProviderContainer>();

            protected override SuggestionsViewModel CreateViewModel()
                => new SuggestionsViewModel(DataSource, Container, TimeService);

            protected void SetProviders(params ISuggestionProvider[] providers)
            {
                Container.Providers.Returns(providers.ToList().AsReadOnly());
            }
        }

        public sealed class TheConstructor : SuggestionsViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ClassData(typeof(ThreeParameterConstructorTestData))]
            public void ThrowsIfAnyOfTheArgumentsIsNull(bool useDataSource, bool useContainer, bool useTimeService)
            {
                var container = useContainer ? Container : null;
                var dataSource = useDataSource ? DataSource : null;
                var timeService = useTimeService ? TimeService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new SuggestionsViewModel(dataSource, container, timeService);

                tryingToConstructWithEmptyParameters
                    .ShouldThrow<ArgumentNullException>();
            }
        }

        public sealed class TheSuggestionsProperty : SuggestionsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task WorksWithSeveralProviders()
            {
                var provider1 = Substitute.For<ISuggestionProvider>();
                var provider2 = Substitute.For<ISuggestionProvider>();
                var suggestion1 = createSuggestion("t1", 12, 9);
                var suggestion2 = createSuggestion("t2", 9, 12);
                var observable1 = Scheduler.CreateColdObservable(createRecorded(0, suggestion1));
                var observable2 = Scheduler.CreateColdObservable(createRecorded(1, suggestion2));
                provider1.GetSuggestions().Returns(observable1);
                provider2.GetSuggestions().Returns(observable2);
                SetProviders(provider1, provider2);

                await ViewModel.Initialize();
                Scheduler.AdvanceTo(1);

                ViewModel.Suggestions.Should().HaveCount(2)
                         .And.Contain(new[] { suggestion1, suggestion2 });
            }

            [Fact, LogIfTooSlow]
            public async Task WorksIfProviderHasMultipleSuggestions()
            {
                var scheduler = new TestScheduler();
                var provider = Substitute.For<ISuggestionProvider>();
                var suggestions = Enumerable.Range(1, 3).Select(createSuggestion).ToArray();
                var observableContent = suggestions
                    .Select(suggestion => createRecorded(1, suggestion))
                    .ToArray();
                var observable = scheduler.CreateColdObservable(observableContent);
                provider.GetSuggestions().Returns(observable);
                SetProviders(provider);

                await ViewModel.Initialize();
                scheduler.AdvanceTo(1);

                ViewModel.Suggestions.Should().HaveCount(suggestions.Length)
                         .And.Contain(suggestions);
            }

            [Fact, LogIfTooSlow]
            public async Task WorksIfProvidersAreEmpty()
            {
                var providers = Enumerable.Range(0, 3)
                    .Select(_ => Substitute.For<ISuggestionProvider>()).ToArray();

                foreach (var provider in providers)
                    provider.GetSuggestions().Returns(Observable.Empty<Suggestion>());

                SetProviders(providers);

                await ViewModel.Initialize();

                ViewModel.Suggestions.Should().HaveCount(0);
            }

            private Suggestion createSuggestion(int index) => createSuggestion($"te{index}", 0, 0);

            private Suggestion createSuggestion(string description, long taskId, long projectId) => new Suggestion(
                TimeEntry.Builder.Create(0)
                    .SetDescription(description)
                    .SetStart(DateTimeOffset.UtcNow)
                    .SetAt(DateTimeOffset.UtcNow)
                    .SetTaskId(taskId)
                    .SetProjectId(projectId)
                    .SetWorkspaceId(11)
                    .SetUserId(12)
                    .Build()
            );

            private Recorded<Notification<Suggestion>> createRecorded(int ticks, Suggestion suggestion)
                => new Recorded<Notification<Suggestion>>(ticks, Notification.CreateOnNext(suggestion));
        }

        public sealed class TheStartTimeEntryCommand : SuggestionsViewModelTest
        {
            [Property]
            public void StarstATimeEntryWithTheSameValuesOfTheSelectedSuggestion(
                NonEmptyString description, long? projectId, long? taskId, long workspaceId)
            {
                var timeEntry = Substitute.For<IDatabaseTimeEntry>();
                timeEntry.Description.Returns(description.Get);
                timeEntry.WorkspaceId.Returns(workspaceId);
                timeEntry.ProjectId.Returns(projectId);
                timeEntry.TaskId.Returns(taskId);
                timeEntry.Duration.Returns((long)TimeSpan.FromHours(1).TotalSeconds);
                var suggestion = new Suggestion(timeEntry);

                ViewModel.StartTimeEntryCommand.ExecuteAsync(suggestion).Wait();

                DataSource.TimeEntries.Received().Start(Arg.Is<StartTimeEntryDTO>(dto =>
                    dto.Description == description.Get &&
                    dto.TaskId == taskId &&
                    dto.ProjectId == projectId &&
                    dto.WorkspaceId == workspaceId
                )).Wait();
            }

            [Fact, LogIfTooSlow]
            public async void InitiatesPushSyncWhenStartingSucceeds()
            {
                var suggestion = createSuggestion();

                await ViewModel.StartTimeEntryCommand.ExecuteAsync(suggestion);

                await DataSource.SyncManager.Received().PushSync();
            }

            [Fact, LogIfTooSlow]
            public async void DoesNotInitiatePushSyncWhenStartingFails()
            {
                var suggestion = createSuggestion();
                DataSource.TimeEntries.Start(Arg.Any<StartTimeEntryDTO>())
                    .Returns(Observable.Throw<IDatabaseTimeEntry>(new Exception()));

                Action executeCommand
                    = () => ViewModel.StartTimeEntryCommand
                                .ExecuteAsync(suggestion)
                                .Wait();

                executeCommand.ShouldThrow<Exception>();
                await DataSource.SyncManager.DidNotReceive().PushSync();
            }

            private Suggestion createSuggestion()
            {
                var timeEntry = Substitute.For<IDatabaseTimeEntry>();
                timeEntry.Duration.Returns((long)TimeSpan.FromMinutes(30).TotalSeconds);
                timeEntry.Description.Returns("Testing");
                return new Suggestion(timeEntry);
            }
        }
    }
}
