using System;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Models;
using Xunit;
using static Toggl.Foundation.Helper.Constants;
using static Toggl.Multivac.Extensions.FunctionalExtensions;
using TextFieldInfo = Toggl.Foundation.Autocomplete.TextFieldInfo;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class StartTimeEntryViewModelTests
    {
        public abstract class StartTimeEntryViewModelTest : BaseViewModelTests<StartTimeEntryViewModel>
        {
            protected const string TagName = "Mobile";

            protected const long TagId = 20;
            protected const long TaskId = 30;
            protected const long ProjectId = 10;
            protected const long WorkspaceId = 40;
            protected const string ProjectName = "Toggl";
            protected const string ProjectColor = "#F41F19";
            protected const string Description = "Testing Toggl mobile apps";

            protected IAutocompleteProvider AutocompleteProvider { get; } = Substitute.For<IAutocompleteProvider>();

            protected StartTimeEntryViewModelTest()
            {
                DataSource.AutocompleteProvider.Returns(AutocompleteProvider);
            }

            protected override void AdditionalSetup()
            {
                DialogService.Confirm(
                   Arg.Any<string>(),
                   Arg.Any<string>(),
                   Arg.Any<string>(),
                   Arg.Any<string>()
               ).Returns(true);
            }

            protected override StartTimeEntryViewModel CreateViewModel()
                => new StartTimeEntryViewModel(TimeService, DialogService, DataSource, NavigationService);
        }

        public sealed class TheConstructor : StartTimeEntryViewModelTest
        {
            [Theory]
            [ClassData(typeof(FourParameterConstructorTestData))]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useDataSource, bool useTimeService, bool useDialogService, bool useNavigationService)
            {
                var dataSource = useDataSource ? DataSource : null;
                var timeService = useTimeService ? TimeService : null;
                var dialogService = useDialogService ? DialogService : null;
                var navigationService = useNavigationService ? NavigationService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new StartTimeEntryViewModel(timeService, dialogService, dataSource, navigationService);

                tryingToConstructWithEmptyParameters
                    .ShouldThrow<ArgumentNullException>();
            }
        }

        public sealed class ThePrepareMethod : StartTimeEntryViewModelTest
        {
            [Fact]
            public void SetsTheDateAccordingToTheDateParameterReceived()
            {
                var date = DateTimeOffset.UtcNow;

                ViewModel.Prepare(date);

                ViewModel.StartTime.Should().BeSameDateAs(date);
            }
        }

        public sealed class TheInitializeMethod : StartTimeEntryViewModelTest
        {
            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public async Task ChecksIfBillableIsAvailableForTheDefaultWorkspace(bool billableValue)
            {
                var workspace = Substitute.For<IDatabaseWorkspace>();
                workspace.Id.Returns(10);
                DataSource.Workspaces.GetDefault()
                    .Returns(Observable.Return(workspace));
                DataSource.Workspaces.WorkspaceHasFeature(10, WorkspaceFeatureId.Pro)
                    .Returns(Observable.Return(billableValue));
                ViewModel.Prepare(DateTimeOffset.UtcNow);

                await ViewModel.Initialize();

                ViewModel.IsBillableAvailable.Should().Be(billableValue);
            }
        }

        public sealed class TheCreateProjectCommand : StartTimeEntryViewModelTest
        {
            private const string currentQuery = "My awesome Toggl project";

            public TheCreateProjectCommand()
            {
                ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor($"Something @{currentQuery}", 15);
            }

            [Fact]
            public async Task CallsTheCreateProjectViewModel()
            {
                await ViewModel.CreateProjectCommand.ExecuteAsync(currentQuery);

                await NavigationService.Received()
                    .Navigate<string, long?>(typeof(EditProjectViewModel), Arg.Any<string>());
            }

            [Fact]
            public async Task UsesTheCurrentQueryAsTheParameterForTheCreateProjectViewModel()
            {
                await ViewModel.CreateProjectCommand.ExecuteAsync(currentQuery);

                await NavigationService.Received()
                    .Navigate<string, long?>(typeof(EditProjectViewModel), currentQuery);
            }
        }

        public sealed class TheBackCommand : StartTimeEntryViewModelTest
        {
            [Fact]
            public async Task ClosesTheViewModel()
            {
                await ViewModel.BackCommand.ExecuteAsync();

                await NavigationService.Received().Close(ViewModel);
            }
        }

        public sealed class TheToggleBillableCommand : StartTimeEntryViewModelTest
        {
            [Fact]
            public void TogglesTheIsBillableProperty()
            {
                var expected = !ViewModel.IsBillable;

                ViewModel.ToggleBillableCommand.Execute();

                ViewModel.IsBillable.Should().Be(expected);
            }
        }

        public sealed class TheChangeStartTimeCommand : StartTimeEntryViewModelTest
        {
            [Property]
            public void CallsTheSelectDateTimeViewModelWithAMinDateThatAllowsUpTo999HoursOfDuration(DateTimeOffset now)
            {
                if (DateTimeOffset.MinValue.AddHours(MaxTimeEntryDurationInHours) <= now ||
                    DateTimeOffset.MaxValue.AddHours(-1) >= now) return;

                TimeService.CurrentDateTime.Returns(now);

                var parameterToReturn = now.AddHours(-2);
                NavigationService
                    .Navigate<DatePickerParameters, DateTimeOffset>(typeof(SelectDateTimeViewModel), Arg.Any<DatePickerParameters>())
                    .Returns(parameterToReturn);
                ViewModel.Prepare(now);

                ViewModel.ChangeStartTimeCommand.ExecuteAsync().Wait();


                NavigationService
                    .Received()
                    .Navigate<DatePickerParameters, DateTimeOffset>(
                        typeof(SelectDateTimeViewModel),
                        Arg.Is<DatePickerParameters>(p => p.MinDate == now.AddHours(-MaxTimeEntryDurationInHours)));
            }

            [Property]
            public void CallsTheSelectDateTimeViewModelWithAMaxDateEqualToTheCurrentDate(DateTimeOffset now)
            {
                if (DateTimeOffset.MinValue.AddHours(MaxTimeEntryDurationInHours) <= now ||
                    DateTimeOffset.MaxValue.AddHours(-1) >= now) return;

                TimeService.CurrentDateTime.Returns(now);

                var parameterToReturn = now.AddHours(-2);
                NavigationService
                    .Navigate<DatePickerParameters, DateTimeOffset>(typeof(SelectDateTimeViewModel), Arg.Any<DatePickerParameters>())
                    .Returns(parameterToReturn);
                ViewModel.Prepare(now);

                ViewModel.ChangeStartTimeCommand.ExecuteAsync().Wait();


                NavigationService
                    .Received()
                    .Navigate<DatePickerParameters, DateTimeOffset>(
                        typeof(SelectDateTimeViewModel),
                        Arg.Is<DatePickerParameters>(p => p.MaxDate == now));
            }

            [Property]
            public void SetsTheStartDateToTheValueReturnedByTheSelectDateTimeDialogViewModel(DateTimeOffset now)
            {
                if (DateTimeOffset.MinValue.AddHours(MaxTimeEntryDurationInHours) <= now ||
                    DateTimeOffset.MaxValue.AddHours(-1) >= now) return;

                var parameterToReturn = now.AddHours(-2);
                NavigationService
                    .Navigate<DateTimeOffset, DateTimeOffset>(typeof(SelectDateTimeViewModel), Arg.Any<DateTimeOffset>())
                    .Returns(parameterToReturn);
                ViewModel.Prepare(now);

                ViewModel.ChangeStartTimeCommand.ExecuteAsync().Wait();

                ViewModel.StartTime.Should().Be(parameterToReturn);
            }

            [Property]
            public void SetsTheIsEditingStartDateToTrueWhileTheViewDoesNotReturnAndThenSetsItBackToFalse(DateTimeOffset now)
            {
                if (DateTimeOffset.MinValue.AddHours(MaxTimeEntryDurationInHours) <= now ||
                    DateTimeOffset.MaxValue.AddHours(-1) >= now) return;

                var parameterToReturn = now.AddHours(-2);
                var tcs = new TaskCompletionSource<DateTimeOffset>();
                NavigationService
                    .Navigate<DateTimeOffset, DateTimeOffset>(typeof(SelectDateTimeViewModel), Arg.Any<DateTimeOffset>())
                    .Returns(tcs.Task);
                ViewModel.Prepare(now);

                var toWait = ViewModel.ChangeStartTimeCommand.ExecuteAsync();
                ViewModel.IsEditingStartDate.Should().BeTrue();
                tcs.SetResult(parameterToReturn);
                toWait.Wait();

                ViewModel.IsEditingStartDate.Should().BeFalse();
            }
        }

        public sealed class TheToggleProjectSuggestionsCommand : StartTimeEntryViewModelTest
        {
            public TheToggleProjectSuggestionsCommand()
            {
                var suggestions = ProjectSuggestion.FromProjects(Enumerable.Empty<IDatabaseProject>());
                AutocompleteProvider
                    .Query(Arg.Is<TextFieldInfo>(info => info.Text.Contains("@")))
                    .Returns(Observable.Return(suggestions));

                AutocompleteProvider
                    .Query(Arg.Is<QueryInfo>(
                        arg => arg.SuggestionType == AutocompleteSuggestionType.Projects))
                    .Returns(Observable.Return(suggestions));
            }

            [Fact]
            public void StartProjectSuggestionEvenIfTheProjectHasAlreadyBeenSelected()
            {
                ViewModel.Prepare(DateTimeOffset.UtcNow);
                ViewModel.TextFieldInfo = TextFieldInfo.Empty
                    .WithTextAndCursor(Description, Description.Length)
                    .WithProjectInfo(ProjectId, ProjectName, ProjectColor);

                ViewModel.ToggleProjectSuggestionsCommand.Execute();

                ViewModel.IsSuggestingProjects.Should().BeTrue();
            }

            [Fact]
            public void SetsTheIsSuggestingProjectsPropertyToTrueIfNotInProjectSuggestionMode()
            {
                ViewModel.Prepare(DateTimeOffset.UtcNow);

                ViewModel.ToggleProjectSuggestionsCommand.Execute();

                ViewModel.IsSuggestingProjects.Should().BeTrue();
            }

            [Fact]
            public void AddsAnAtSymbolAtTheEndOfTheQueryInOrderToStartProjectSuggestionMode()
            {
                const string description = "Testing Toggl Apps";
                var expected = $"{description}@";
                ViewModel.Prepare(DateTimeOffset.UtcNow);
                ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, description.Length);

                ViewModel.ToggleProjectSuggestionsCommand.Execute();

                ViewModel.TextFieldInfo.Text.Should().Be(expected);
            }

            [Theory]
            [InlineData("@")]
            [InlineData("@somequery")]
            [InlineData("@some query")]
            [InlineData("@some query@query")]
            [InlineData("Testing Toggl Apps @")]
            [InlineData("Testing Toggl Apps @somequery")]
            [InlineData("Testing Toggl Apps @some query")]
            [InlineData("Testing Toggl Apps @some query@query")]
            [InlineData("Testing Toggl Apps@")]
            [InlineData("Testing Toggl Apps@somequery")]
            [InlineData("Testing Toggl Apps@some query")]
            [InlineData("Testing Toggl Apps@some query@query")]
            public void SetsTheIsSuggestingProjectsPropertyToFalseIfAlreadyInProjectSuggestionMode(string description)
            {
                ViewModel.Prepare(DateTimeOffset.UtcNow);
                ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, description.Length);

                ViewModel.ToggleProjectSuggestionsCommand.Execute();

                ViewModel.IsSuggestingProjects.Should().BeFalse();
            }

            [Theory]
            [InlineData("@", "")]
            [InlineData("@somequery", "")]
            [InlineData("@some query", "")]
            [InlineData("@some query@query", "")]
            [InlineData("Testing Toggl Apps @", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps @somequery", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps @some query", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps @some query@query", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps@", "Testing Toggl Apps")]
            [InlineData("Testing Toggl Apps@somequery", "Testing Toggl Apps")]
            [InlineData("Testing Toggl Apps@some query", "Testing Toggl Apps")]
            [InlineData("Testing Toggl Apps@some query@query", "Testing Toggl Apps")]
            public void RemovesTheAtSymbolFromTheDescriptionTextIfAlreadyInProjectSuggestionMode(
                string description, string expected)
            {
                ViewModel.Prepare(DateTimeOffset.UtcNow);
                ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, description.Length);

                ViewModel.ToggleProjectSuggestionsCommand.Execute();

                ViewModel.TextFieldInfo.Text.Should().Be(expected);
            }
        }

        public sealed class TheToggleTagSuggestionsCommand : StartTimeEntryViewModelTest
        {
            public TheToggleTagSuggestionsCommand()
            {
                var tag = Substitute.For<IDatabaseTag>();
                tag.Id.Returns(TagId);
                tag.Name.Returns(TagName);
                var suggestions = TagSuggestion.FromTags(new[] { tag });
                AutocompleteProvider
                    .Query(Arg.Is<TextFieldInfo>(info => info.Text.Contains("#")))
                    .Returns(Observable.Return(suggestions));
            }

            [Fact]
            public void SetsTheIsSuggestingTagsPropertyToTrueIfNotInTagSuggestionMode()
            {
                ViewModel.Prepare(DateTimeOffset.UtcNow);

                ViewModel.ToggleTagSuggestionsCommand.Execute();

                ViewModel.IsSuggestingTags.Should().BeTrue();
            }

            [Fact]
            public void AddsHashtagSymbolAtTheEndOfTheQueryInOrderToTagSuggestionMode()
            {
                const string description = "Testing Toggl Apps";
                var expected = $"{description}#";
                ViewModel.Prepare(DateTimeOffset.UtcNow);
                ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, description.Length);

                ViewModel.ToggleTagSuggestionsCommand.Execute();

                ViewModel.TextFieldInfo.Text.Should().Be(expected);
            }

            [Theory]
            [InlineData("#")]
            [InlineData("#somequery")]
            [InlineData("#some query")]
            [InlineData("#some quer#query")]
            [InlineData("Testing Toggl Apps #")]
            [InlineData("Testing Toggl Apps #somequery")]
            [InlineData("Testing Toggl Apps #some query")]
            [InlineData("Testing Toggl Apps #some query#query")]
            [InlineData("Testing Toggl Apps#")]
            [InlineData("Testing Toggl Apps#somequery")]
            [InlineData("Testing Toggl Apps#some query")]
            [InlineData("Testing Toggl Apps#some query#query")]
            public void SetsTheIsSuggestingTagsPropertyToFalseIfAlreadyInTagSuggestionMode(string description)
            {
                ViewModel.Prepare(DateTimeOffset.UtcNow);
                ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, description.Length);

                ViewModel.ToggleTagSuggestionsCommand.Execute();

                ViewModel.IsSuggestingTags.Should().BeFalse();
            }

            [Theory]
            [InlineData("#", "")]
            [InlineData("#somequery", "")]
            [InlineData("#some query", "")]
            [InlineData("#some query#query", "")]
            [InlineData("Testing Toggl Apps #", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps #somequery", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps #some query", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps #some query#query", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps#", "Testing Toggl Apps")]
            [InlineData("Testing Toggl Apps#somequery", "Testing Toggl Apps")]
            [InlineData("Testing Toggl Apps#some query", "Testing Toggl Apps")]
            [InlineData("Testing Toggl Apps#some query@query", "Testing Toggl Apps")]
            public void RemovesTheHashtagSymbolFromTheDescriptionTextIfAlreadyInTagSuggestionMode(
                string description, string expected)
            {
                ViewModel.Prepare(DateTimeOffset.UtcNow);
                ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, description.Length);

                ViewModel.ToggleTagSuggestionsCommand.Execute();

                ViewModel.TextFieldInfo.Text.Should().Be(expected);
            }
        }

        public sealed class TheChangeDurationCommandCommand : StartTimeEntryViewModelTest
        {
            [Fact]
            public async Task SetsTheStartDateToTheValueReturnedByTheSelectDateTimeDialogViewModel()
            {
                var now = DateTimeOffset.UtcNow;
                var parameterToReturn = DurationParameter.WithStartAndStop(now.AddHours(-2), null);
                NavigationService
                    .Navigate<DurationParameter, DurationParameter>(typeof(EditDurationViewModel), Arg.Any<DurationParameter>())
                    .Returns(parameterToReturn);
                ViewModel.Prepare(now);

                await ViewModel.ChangeDurationCommand.ExecuteAsync();

                ViewModel.StartTime.Should().Be(parameterToReturn.Start);
            }

            [Fact]
            public async Task SetsTheIsEditingDurationDateToTrueWhileTheViewDoesNotReturnAndThenSetsItBackToFalse()
            {
                var now = DateTimeOffset.UtcNow;
                var parameterToReturn = DurationParameter.WithStartAndStop(now.AddHours(-2), null);
                var tcs = new TaskCompletionSource<DurationParameter>();
                NavigationService
                    .Navigate<DurationParameter, DurationParameter>(typeof(EditDurationViewModel), Arg.Any<DurationParameter>())
                    .Returns(tcs.Task);
                ViewModel.Prepare(now);

                var toWait = ViewModel.ChangeDurationCommand.ExecuteAsync();
                ViewModel.IsEditingDuration.Should().BeTrue();
                tcs.SetResult(parameterToReturn);
                await toWait;

                ViewModel.IsEditingDuration.Should().BeFalse();
            }
        }

        public sealed class TheDoneCommand : StartTimeEntryViewModelTest
        {
            public sealed class StartsANewTimeEntry : StartTimeEntryViewModelTest
            {
                private const long taskId = 9;
                private const long userId = 10;
                private const long projectId = 11;
                private const long defaultWorkspaceId = 12;
                private const long projectWorkspaceId = 13;
                private const string description = "Testing Toggl apps";

                private readonly IDatabaseUser user = Substitute.For<IDatabaseUser>();
                private readonly IDatabaseProject project = Substitute.For<IDatabaseProject>();

                private readonly DateTimeOffset startDate = DateTimeOffset.UtcNow;

                public StartsANewTimeEntry()
                {
                    user.Id.Returns(userId);
                    user.DefaultWorkspaceId.Returns(defaultWorkspaceId);
                    DataSource.User.Current()
                        .Returns(Observable.Return(user));

                    project.Id.Returns(projectId);
                    project.WorkspaceId.Returns(projectWorkspaceId);
                    DataSource.Projects
                         .GetById(projectId)
                         .Returns(Observable.Return(project));

                    ViewModel.Prepare(startDate);
                }

                [Fact]
                public async Task WithTheDatePassedWhenNavigatingToTheViewModel()
                {
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, 0);

                    ViewModel.DoneCommand.Execute();

                    await DataSource.TimeEntries.Received().Start(Arg.Is<StartTimeEntryDTO>(dto =>
                        dto.StartTime == startDate
                    ));
                }

                [Theory]
                [InlineData(true)]
                [InlineData(false)]
                public async Task WithTheAppropriateValueForBillable(bool billable)
                {
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, 0);
                    if (billable != ViewModel.IsBillable)
                        ViewModel.ToggleBillableCommand.Execute();

                    ViewModel.DoneCommand.Execute();

                    await DataSource.TimeEntries.Received().Start(Arg.Is<StartTimeEntryDTO>(dto =>
                        dto.Billable == billable
                    ));
                }

                [Fact]
                public async Task WithTheDefaultWorkspaceIfNoProjectIsProvided()
                {
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, 0);

                    ViewModel.DoneCommand.Execute();

                    await DataSource.TimeEntries.Received().Start(Arg.Is<StartTimeEntryDTO>(dto =>
                        dto.WorkspaceId == defaultWorkspaceId
                    ));
                }

                [Fact]
                public async Task WithTheProjectWorkspaceIfAProjectIsProvided()
                {
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty
                        .WithTextAndCursor(description, 0)
                        .WithProjectInfo(projectId, "Something", "#123123");

                    ViewModel.DoneCommand.Execute();

                    await DataSource.TimeEntries.Received().Start(Arg.Is<StartTimeEntryDTO>(dto =>
                        dto.WorkspaceId == projectWorkspaceId
                    ));
                }

                [Fact]
                public async Task WithTheAppropriateWorkspaceSelectedIfNoProjectWasTapped()
                {
                    const long expectedWorkspace = 1234;
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty
                        .WithTextAndCursor(description, 0)
                        .WithProjectInfo(projectId, "Something", "#123123");
                    ViewModel.SelectSuggestionCommand
                             .Execute(ProjectSuggestion.NoProject(expectedWorkspace, ""));

                    ViewModel.DoneCommand.Execute();

                    await DataSource.TimeEntries.Received().Start(Arg.Is<StartTimeEntryDTO>(dto =>
                        dto.WorkspaceId == expectedWorkspace
                    ));
                }

                [Fact]
                public async Task WithTheCurrentUsersId()
                {
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, 0);
                    ViewModel.DoneCommand.Execute();

                    await DataSource.TimeEntries.Received().Start(Arg.Is<StartTimeEntryDTO>(dto =>
                        dto.UserId == userId
                    ));
                }

                [Fact]
                public async Task WithTheAppropriateProjectId()
                {
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty
                        .WithTextAndCursor(description, 0)
                        .WithProjectInfo(projectId, "Something", "#123123");

                    ViewModel.DoneCommand.Execute();

                    await DataSource.TimeEntries.Received().Start(Arg.Is<StartTimeEntryDTO>(dto =>
                        dto.ProjectId == projectId
                    ));
                }

                [Fact]
                public async Task WithTheAppropriateDescription()
                {
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, 0);

                    ViewModel.DoneCommand.Execute();

                    await DataSource.TimeEntries.Received().Start(Arg.Is<StartTimeEntryDTO>(dto =>
                        dto.Description == description
                    ));
                }

                [Fact]
                public async Task WithTheAppropriateTaskId()
                {
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty
                        .WithTextAndCursor(description, 0)
                        .WithProjectAndTaskInfo(projectId, "Something", "#AABBCC", taskId, "Some task");

                    ViewModel.DoneCommand.Execute();

                    await DataSource.TimeEntries.Received().Start(Arg.Is<StartTimeEntryDTO>(dto =>
                        dto.TaskId == taskId
                    ));
                }

                [Fact]
                public async Task WithTheSelectedTags()
                {
                    var tags = Enumerable.Range(0, 3).Select(tagSuggestionFromInt);
                    var expectedTags = tags.Select(tag => tag.TagId).ToArray();
                    ViewModel.TextFieldInfo =
                        tags.Aggregate(TextFieldInfo.Empty.WithTextAndCursor(description, 0),
                            (info, tag) => info.AddTag(tag));

                    ViewModel.DoneCommand.Execute();

                    await DataSource.TimeEntries.Received().Start(Arg.Is<StartTimeEntryDTO>(dto =>
                        dto.TagIds.Length == expectedTags.Length &&
                        dto.TagIds.All(tagId => expectedTags.Contains(tagId))
                    ));
                }

                [Fact]
                public async Task InitiatesPushSync()
                {
                    ViewModel.DoneCommand.Execute();

                    await DataSource.SyncManager.Received().PushSync();
                }

                [Fact]
                public async Task DoesNotInitiatePushSyncWhenSavingFails()
                {
                    DataSource.TimeEntries.Start(Arg.Any<StartTimeEntryDTO>())
                        .Returns(Observable.Throw<IDatabaseTimeEntry>(new Exception()));

                    ViewModel.DoneCommand.Execute();

                    await DataSource.SyncManager.DidNotReceive().PushSync();
                }

                private TagSuggestion tagSuggestionFromInt(int i)
                {
                    var tag = Substitute.For<IDatabaseTag>();
                    tag.Id.Returns(i);
                    tag.Name.Returns(i.ToString());

                    return new TagSuggestion(tag);
                }
            }

            [Fact]
            public async Task ClosesTheViewModel()
            {
                await ViewModel.DoneCommand.ExecuteAsync();

                await NavigationService.Received().Close(ViewModel);
            }
        }

        public sealed class TheSelectSuggestionCommand
        {
            public abstract class SelectSuggestionTest<TSuggestion> : StartTimeEntryViewModelTest
                where TSuggestion : AutocompleteSuggestion
            {
                protected IDatabaseTag Tag { get; }
                protected IDatabaseTask Task { get; }
                protected IDatabaseProject Project { get; }
                protected IDatabaseTimeEntry TimeEntry { get; }
                protected IDatabaseWorkspace Workspace { get; }

                protected abstract TSuggestion Suggestion { get; }

                protected SelectSuggestionTest()
                {
                    Workspace = Substitute.For<IDatabaseWorkspace>();
                    Workspace.Id.Returns(WorkspaceId);

                    Project = Substitute.For<IDatabaseProject>();
                    Project.Id.Returns(ProjectId);
                    Project.Name.Returns(ProjectName);
                    Project.Color.Returns(ProjectColor);
                    Project.Workspace.Returns(Workspace);
                    Project.WorkspaceId.Returns(WorkspaceId);

                    Task = Substitute.For<IDatabaseTask>();
                    Task.Id.Returns(TaskId);
                    Task.Project.Returns(Project);
                    Task.ProjectId.Returns(ProjectId);
                    Task.WorkspaceId.Returns(WorkspaceId);
                    Task.Name.Returns(TaskId.ToString());

                    TimeEntry = Substitute.For<IDatabaseTimeEntry>();
                    TimeEntry.Description.Returns(Description);
                    TimeEntry.Project.Returns(Project);

                    Tag = Substitute.For<IDatabaseTag>();
                    Tag.Id.Returns(TagId);
                    Tag.Name.Returns(TagName);
                }
            }

            public abstract class ProjectSettingSuggestion<TSuggestion> : SelectSuggestionTest<TSuggestion>
                where TSuggestion : AutocompleteSuggestion
            {
                [Fact]
                public void SetsTheProjectIdToTheSuggestedProjectId()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.ProjectId.Should().Be(ProjectId);
                }

                [Fact]
                public void SetsTheProjectNameToTheSuggestedProjectName()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.ProjectName.Should().Be(ProjectName);
                }

                [Fact]
                public void SetsTheProjectColorToTheSuggestedProjectColor()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.ProjectColor.Should().Be(ProjectColor);
                }

                [Theory]
                [InlineData(true)]
                [InlineData(false)]
                [InlineData(null)]
                public void SetsTheAppropriateBillableValue(bool? billableValue)
                {
                    Project.Billable.Returns(billableValue);
                    DataSource.Projects.GetById(ProjectId).Returns(Observable.Return(Project));
                    DataSource.Workspaces.GetById(WorkspaceId).Returns(Observable.Return(Workspace));
                    DataSource.Workspaces.WorkspaceHasFeature(WorkspaceId, WorkspaceFeatureId.Pro)
                        .Returns(Observable.Return(true));

                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.IsBillable.Should().Be(billableValue ?? false);
                    ViewModel.IsBillableAvailable.Should().BeTrue();
                }

                [Theory]
                [InlineData(true)]
                [InlineData(false)]
                [InlineData(null)]
                public void DisablesBillableIfTheWorkspaceOfTheSelectedProjectDoesNotAllowIt(bool? billableValue)
                {
                    Project.Billable.Returns(billableValue);
                    DataSource.Projects.GetById(ProjectId).Returns(Observable.Return(Project));
                    DataSource.Workspaces.GetById(WorkspaceId).Returns(Observable.Return(Workspace));
                    DataSource.Workspaces.WorkspaceHasFeature(WorkspaceId, WorkspaceFeatureId.Pro)
                        .Returns(Observable.Return(false));

                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.IsBillable.Should().BeFalse();
                    ViewModel.IsBillableAvailable.Should().BeFalse();
                }
            }

            public abstract class ProjectTaskSuggestion<TSuggestion> : ProjectSettingSuggestion<TSuggestion>
                where TSuggestion : AutocompleteSuggestion
            {
                protected ProjectTaskSuggestion()
                {
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor("Something @togg", 15);
                }

                [Fact]
                public void RemovesTheProjectQueryFromTheTextFieldInfo()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.Text.Should().Be("Something ");
                }

                [Fact]
                public async Task ShowsConfirmDialogIfWorkspaceIsAboutToBeChanged()
                {
                    var user = Substitute.For<IDatabaseUser>();
                    user.DefaultWorkspaceId.Returns(100);
                    DataSource.User.Current().Returns(Observable.Return(user));
                    await ViewModel.Initialize();

                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    await DialogService.Received().Confirm(
                        Arg.Is(Resources.DifferentWorkspaceAlertTitle),
                        Arg.Is(Resources.DifferentWorkspaceAlertMessage),
                        Arg.Is(Resources.Ok),
                        Arg.Is(Resources.Cancel)
                    );
                }

                [Fact]
                public async Task DoesNotShowConfirmDialogIfWorkspaceIsNotGoingToChange()
                {
                    var user = Substitute.For<IDatabaseUser>();
                    user.DefaultWorkspaceId.Returns(WorkspaceId);
                    DataSource.User.Current().Returns(Observable.Return(user));
                    await ViewModel.Initialize();

                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    await DialogService.DidNotReceive().Confirm(
                        Arg.Any<string>(),
                        Arg.Any<string>(),
                        Arg.Any<string>(),
                        Arg.Any<string>()
                    );
                }

                [Fact]
                public async Task ClearsTagsIfWorkspaceIsChanged()
                {
                    var user = Substitute.For<IDatabaseUser>();
                    user.DefaultWorkspaceId.Returns(100);
                    DataSource.User.Current().Returns(Observable.Return(user));
                    await ViewModel.Initialize();
                    Enumerable.Range(100, 10)
                        .Select(i =>
                        {
                            var tag = Substitute.For<IDatabaseTag>();
                            tag.Id.Returns(i);
                            return new TagSuggestion(tag);
                        }).ForEach(ViewModel.SelectSuggestionCommand.Execute);

                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.Tags.Should().BeEmpty();
                }
            }

            public sealed class WhenSelectingATimeEntrySuggestion : ProjectSettingSuggestion<TimeEntrySuggestion>
            {
                protected override TimeEntrySuggestion Suggestion { get; }

                public WhenSelectingATimeEntrySuggestion()
                {
                    Suggestion = new TimeEntrySuggestion(TimeEntry);
                }

                [Fact]
                public void SetsTheTextFieldInfoTextToTheValueOfTheSuggestedDescription()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.Text.Should().Be(Description);
                }
            }

            public sealed class WhenSelectingATaskSuggestion : ProjectTaskSuggestion<TaskSuggestion>
            {
                protected override TaskSuggestion Suggestion { get; }

                public WhenSelectingATaskSuggestion()
                {
                    Suggestion = new TaskSuggestion(Task);
                }

                [Fact]
                public void SetsTheTaskIdToTheSameIdAsTheSelectedSuggestion()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.TaskId.Should().Be(TaskId);
                }
            }

            public sealed class WhenSelectingAProjectSuggestion : ProjectTaskSuggestion<ProjectSuggestion>
            {
                protected override ProjectSuggestion Suggestion { get; }

                public WhenSelectingAProjectSuggestion()
                {
                    Suggestion = new ProjectSuggestion(Project);
                }

                [Fact]
                public void SetsTheTaskIdToNull()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.TaskId.Should().BeNull();
                }
            }

            public sealed class WhenSelectingATagSuggestion : SelectSuggestionTest<TagSuggestion>
            {
                protected override TagSuggestion Suggestion { get; }

                public WhenSelectingATagSuggestion()
                {
                    Suggestion = new TagSuggestion(Tag);

                    ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor("Something #togg", 15);
                }

                [Fact]
                public void RemovesTheTagQueryFromTheTextFieldInfo()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.Text.Should().Be("Something ");
                }

                [Fact]
                public void AddsTheSuggestedTagToTheList()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.Tags.Should().Contain(Suggestion);
                }
            }

            public sealed class WhenSelectingAQuerySymbolSuggestion : SelectSuggestionTest<QuerySymbolSuggestion>
            {
                protected override QuerySymbolSuggestion Suggestion { get; } = QuerySymbolSuggestion.Suggestions.First();

                [Fact]
                public void SetsTheTextToTheQuerySymbolSelected()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.Text.Should().Be(Suggestion.Symbol);
                }
            }
        }

        public sealed class TheSuggestionsProperty : StartTimeEntryViewModelTest
        {
            [Fact]
            public void IsClearedWhenThereAreNoWordsToQuery()
            {
                ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor("", 0);

                ViewModel.Suggestions.Should().HaveCount(0);
            }
        }

        public sealed class TheDescriptionRemainingBytesProperty : StartTimeEntryViewModelTest
        {
            [Fact]
            public void IsMaxIfTheTextIsEmpty()
            {
                ViewModel.TextFieldInfo = TextFieldInfo.Empty;

                ViewModel.DescriptionRemainingBytes.Should()
                    .Be(MaxTimeEntryDescriptionLengthInBytes);
            }

            [Theory]
            [InlineData("Hello fox")]
            [InlineData("Some emojis: 🔥😳👻")]
            [InlineData("Some weird characters: āčēļķīņš")]
            [InlineData("Some random arabic characters: ظۓڰڿڀ")]
            public void IsDecreasedForEachByteInTheText(string text)
            {
                var expectedRemainingByteCount
                    = MaxTimeEntryDescriptionLengthInBytes - Encoding.UTF8.GetByteCount(text);

                ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(text, 0);

                ViewModel.DescriptionRemainingBytes.Should()
                    .Be(expectedRemainingByteCount);
            }

            [Fact]
            public void IsNegativeWhenTextLengthExceedsMax()
            {
                var bytesOverLimit = 5;
                var longString = new string('0', MaxTimeEntryDescriptionLengthInBytes + bytesOverLimit);

                ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(longString, 0);

                ViewModel.DescriptionRemainingBytes.Should().Be(-bytesOverLimit);
            }
        }

        public sealed class TheDescriptionLengthExceededproperty : StartTimeEntryViewModelTest
        {
            [Theory]
            [InlineData(0)]
            [InlineData(20)]
            [InlineData(2999)]
            [InlineData(3000)]
            public void IsFalseIfTextIsShorterOrEqualToMax(int byteCount)
            {
                var text = new string('0', byteCount);

                ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(text, 0);

                ViewModel.DescriptionLengthExceeded.Should().BeFalse();
            }

            [Theory]
            [InlineData(MaxTimeEntryDescriptionLengthInBytes + 1)]
            [InlineData(MaxTimeEntryDescriptionLengthInBytes + 20)]
            public void IsTrueWhenTextIsLongerThanMax(int byteCount)
            {
                var text = new string('0', byteCount);

                ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(text, 0);

                ViewModel.DescriptionLengthExceeded.Should().BeTrue();
            }
        }
    }
}
