using System;
using System.Collections.Generic;
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
using static Toggl.Multivac.Extensions.StringExtensions;
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
            [Theory, LogIfTooSlow]
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
            [Fact, LogIfTooSlow]
            public void SetsTheDateAccordingToTheDateParameterReceived()
            {
                var date = DateTimeOffset.UtcNow;

                ViewModel.Prepare(date);

                ViewModel.StartTime.Should().BeSameDateAs(date);
            }
        }

        public sealed class TheInitializeMethod : StartTimeEntryViewModelTest
        {
            [Theory, LogIfTooSlow]
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

        public abstract class TheSuggestCreationProperty : StartTimeEntryViewModelTest
        {
            protected abstract int MaxLength { get; }
            protected abstract char QuerySymbol { get; }
            protected abstract string QueryWithExactSuggestionMatch { get; }

            public TheSuggestCreationProperty()
            {
                DataSource.AutocompleteProvider
                    .ParseFieldInfo(Arg.Is<TextFieldInfo>(info => !info.Text.Contains("@") && !info.Text.Contains("#")))
                    .Returns(x => new QueryInfo(x.Arg<TextFieldInfo>().Text, AutocompleteSuggestionType.TimeEntries));
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfSuggestingTimeEntries()
            {
                await ViewModel.Initialize();

                ViewModel.TextFieldInfo = ViewModel.TextFieldInfo.WithTextAndCursor("", 1);

                ViewModel.SuggestCreation.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfTheCurrentQueryIsEmpty()
            {
                await ViewModel.Initialize();

                ViewModel.TextFieldInfo = ViewModel.TextFieldInfo.WithTextAndCursor($"{QuerySymbol}", 1);

                ViewModel.SuggestCreation.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfTheCurrentQueryIsOnlyWhitespace()
            {
                await ViewModel.Initialize();

                ViewModel.TextFieldInfo = ViewModel.TextFieldInfo.WithTextAndCursor($"{QuerySymbol}    ", 1);

                ViewModel.SuggestCreation.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfTheCurrentQueryIsLongerThanMaxLength()
            {
                await ViewModel.Initialize();

                ViewModel.TextFieldInfo = ViewModel.TextFieldInfo
                    .WithTextAndCursor($"{QuerySymbol}{createLongString(MaxLength + 1)}", 1);
                
                ViewModel.SuggestCreation.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfSuchSuggestionAlreadyExists()
            {
                await ViewModel.Initialize();

                ViewModel.TextFieldInfo = ViewModel.TextFieldInfo
                    .WithTextAndCursor($"{QuerySymbol}{QueryWithExactSuggestionMatch}", 1);

                ViewModel.SuggestCreation.Should().BeFalse();
            }

            private string createLongString(int length)
                => Enumerable
                    .Range(0, length)
                    .Aggregate(new StringBuilder(), (builder, _) => builder.Append('A'))
                    .ToString();


            public sealed class WhenSuggestingProjects : TheSuggestCreationProperty
            {
                protected override int MaxLength => MaxProjectNameLengthInBytes;
                protected override char QuerySymbol => '@';
                protected override string QueryWithExactSuggestionMatch => ProjectName;

                public WhenSuggestingProjects()
                {
                    var project = Substitute.For<IDatabaseProject>();
                    project.Id.Returns(10);
                    project.Name.Returns(ProjectName);
                    project.WorkspaceId.Returns(40);
                    project.Workspace.Name.Returns("Some workspace");
                    var projectSuggestion = new ProjectSuggestion(project);

                    DataSource.AutocompleteProvider
                        .Query(Arg.Is<QueryInfo>(info => info.SuggestionType == AutocompleteSuggestionType.Projects))
                              .Returns(Observable.Return(new ProjectSuggestion[] { projectSuggestion }));

                    DataSource.AutocompleteProvider
                        .ParseFieldInfo(Arg.Is<TextFieldInfo>(info => info.Text.Contains("@")))
                        .Returns(x => new QueryInfo(x.Arg<TextFieldInfo>().Text.Replace("@", ""), AutocompleteSuggestionType.Projects));

                    ViewModel.Prepare(DateTimeOffset.Now);
                }

                [Fact, LogIfTooSlow]
                public async Task ReturnsFalseIfAProjectIsAlreadySelected()
                {
                    await ViewModel.Initialize();
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty
                        .WithProjectInfo(WorkspaceId, ProjectId, ProjectName, ProjectColor);

                    ViewModel.TextFieldInfo = ViewModel.TextFieldInfo.WithTextAndCursor("abcde @fgh", 10);

                    ViewModel.SuggestCreation.Should().BeFalse();
                }
            }

            public sealed class WhenSuggestingTags : TheSuggestCreationProperty
            {
                protected override int MaxLength => MaxTagNameLengthInBytes;
                protected override char QuerySymbol => '#';
                protected override string QueryWithExactSuggestionMatch => TagName;

                public WhenSuggestingTags()
                {
                    var tag = Substitute.For<IDatabaseTag>();
                    tag.Id.Returns(20);
                    tag.Name.Returns(TagName);
                    var tagSuggestion = new TagSuggestion(tag);

                    DataSource.AutocompleteProvider
                        .Query(Arg.Is<QueryInfo>(info => info.SuggestionType == AutocompleteSuggestionType.Tags))
                        .Returns(Observable.Return(new TagSuggestion[] { tagSuggestion }));

                    DataSource.AutocompleteProvider
                        .ParseFieldInfo(Arg.Is<TextFieldInfo>(info => info.Text.Contains("#")))
                        .Returns(x => new QueryInfo(x.Arg<TextFieldInfo>().Text.Replace("#", ""), AutocompleteSuggestionType.Tags));

                    ViewModel.Prepare(DateTimeOffset.Now);
                }
            }
        }

        public sealed class TheCreateProjectCommand
        {
            public sealed class WhenSuggestingProjects : StartTimeEntryViewModelTest
            {
                private const string currentQuery = "My awesome Toggl project";

                public WhenSuggestingProjects()
                {
                    DataSource.AutocompleteProvider
                        .ParseFieldInfo(Arg.Is<TextFieldInfo>(info => info.Text.Contains("@")))
                              .Returns(x => new QueryInfo(x.Arg<TextFieldInfo>().Text.Replace("@", ""), AutocompleteSuggestionType.Projects));

                    var project = Substitute.For<IDatabaseProject>();
                    project.Id.Returns(10);
                    DataSource.Projects.GetById(Arg.Any<long>()).Returns(Observable.Return(project));
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor($"@{currentQuery}", 15);

                    ViewModel.Prepare(DateTimeOffset.UtcNow);
                }

                [Fact, LogIfTooSlow]
                public async Task CallsTheCreateProjectViewModel()
                {
                    await ViewModel.Initialize();
                    
                    await ViewModel.CreateCommand.ExecuteAsync();

                    await NavigationService.Received()
                        .Navigate<string, long?>(typeof(EditProjectViewModel), Arg.Any<string>());
                }

                [Fact, LogIfTooSlow]
                public async Task UsesTheCurrentQueryAsTheParameterForTheCreateProjectViewModel()
                {
                    await ViewModel.Initialize();

                    await ViewModel.CreateCommand.ExecuteAsync();

                    await NavigationService.Received()
                        .Navigate<string, long?>(typeof(EditProjectViewModel), currentQuery);
                }

                [Fact, LogIfTooSlow]
                public async Task SelectsTheCreatedProject()
                {
                    long projectId = 200;
                    NavigationService
                        .Navigate<string, long?>(typeof(EditProjectViewModel), Arg.Is(currentQuery))
                        .Returns(projectId);
                    var project = Substitute.For<IDatabaseProject>();
                    project.Id.Returns(projectId);
                    project.Name.Returns(currentQuery);
                    DataSource.Projects.GetById(Arg.Is(projectId)).Returns(Observable.Return(project));
                    await ViewModel.Initialize();

                    await ViewModel.CreateCommand.ExecuteAsync();

                    ViewModel.TextFieldInfo.ProjectName.Should().Be(currentQuery);
                }
            }

            public sealed class WhenSuggestingTags : StartTimeEntryViewModelTest
            {
                private const string currentQuery = "My awesome Toggl project";

                public WhenSuggestingTags()
                {
                    DataSource.AutocompleteProvider
                        .ParseFieldInfo(Arg.Is<TextFieldInfo>(info => info.Text.Contains("#")))
                              .Returns(x => new QueryInfo(x.Arg<TextFieldInfo>().Text.Replace("#", ""), AutocompleteSuggestionType.Tags));
                    
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor($"#{currentQuery}", 1);

                    ViewModel.Prepare(DateTimeOffset.UtcNow);
                }

                [Fact, LogIfTooSlow]
                public async Task CreatesTagWithCurrentQueryAsName()
                {
                    await ViewModel.Initialize();

                    await ViewModel.CreateCommand.ExecuteAsync();

                    await DataSource.Tags.Received()
                        .Create(Arg.Is(currentQuery), Arg.Any<long>());
                }

                [Fact, LogIfTooSlow]
                public async Task CreatesTagInProjectsWorkspaceIfAProjectIsSelected()
                {
                    long workspaceId = 100;
                    long projectId = 101;
                    var project = Substitute.For<IDatabaseProject>();
                    project.Id.Returns(projectId);
                    project.WorkspaceId.Returns(workspaceId);
                    DataSource.Projects.GetById(Arg.Is(projectId))
                        .Returns(Observable.Return(project));
                    await ViewModel.Initialize();
                    ViewModel.TextFieldInfo = ViewModel.TextFieldInfo
                        .WithProjectInfo(workspaceId, projectId, "Project", "0000AF");

                    await ViewModel.CreateCommand.ExecuteAsync();

                    await DataSource.Tags.Received()
                        .Create(Arg.Any<string>(), Arg.Is(workspaceId));
                }

                [Fact, LogIfTooSlow]
                public async Task CreatesTagInUsersDefaultWorkspaceIfNoProjectIsSelected()
                {
                    long workspaceId = 100;
                    var user = Substitute.For<IDatabaseUser>();
                    user.DefaultWorkspaceId.Returns(workspaceId);
                    DataSource.User.Current().Returns(Observable.Return(user));
                    await ViewModel.Initialize();

                    await ViewModel.CreateCommand.ExecuteAsync();

                    await DataSource.Tags.Received()
                        .Create(Arg.Any<string>(), Arg.Is(workspaceId));
                }

                [Fact, LogIfTooSlow]
                public async Task SelectsTheCreatedTag()
                {
                    DataSource.Tags.Create(Arg.Any<string>(), Arg.Any<long>())
                        .Returns(callInfo =>
                        {
                            var tag = Substitute.For<IDatabaseTag>();
                            tag.Name.Returns(callInfo.Arg<string>());
                            return Observable.Return(tag);
                        });
                    await ViewModel.Initialize();

                    await ViewModel.CreateCommand.ExecuteAsync();

                    ViewModel.TextFieldInfo.Tags.Should()
                        .Contain(tag => tag.Name == currentQuery);
                }
            }
        }

        public sealed class TheBackCommand : StartTimeEntryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                await ViewModel.BackCommand.ExecuteAsync();

                await NavigationService.Received().Close(ViewModel);
            }
        }

        public sealed class TheToggleBillableCommand : StartTimeEntryViewModelTest
        {
            [Fact, LogIfTooSlow]
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

            [Fact, LogIfTooSlow]
            public void StartProjectSuggestionEvenIfTheProjectHasAlreadyBeenSelected()
            {
                ViewModel.Prepare(DateTimeOffset.UtcNow);
                ViewModel.TextFieldInfo = TextFieldInfo.Empty
                    .WithTextAndCursor(Description, Description.Length)
                    .WithProjectInfo(WorkspaceId, ProjectId, ProjectName, ProjectColor);

                ViewModel.ToggleProjectSuggestionsCommand.Execute();

                ViewModel.IsSuggestingProjects.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void SetsTheIsSuggestingProjectsPropertyToTrueIfNotInProjectSuggestionMode()
            {
                ViewModel.Prepare(DateTimeOffset.UtcNow);

                ViewModel.ToggleProjectSuggestionsCommand.Execute();

                ViewModel.IsSuggestingProjects.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void AddsAnAtSymbolAtTheEndOfTheQueryInOrderToStartProjectSuggestionMode()
            {
                const string description = "Testing Toggl Apps";
                var expected = $"{description}@";
                ViewModel.Prepare(DateTimeOffset.UtcNow);
                ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, description.Length);

                ViewModel.ToggleProjectSuggestionsCommand.Execute();

                ViewModel.TextFieldInfo.Text.Should().Be(expected);
            }

            [Theory, LogIfTooSlow]
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

            [Theory, LogIfTooSlow]
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

            [Fact, LogIfTooSlow]
            public void SetsTheIsSuggestingTagsPropertyToTrueIfNotInTagSuggestionMode()
            {
                ViewModel.Prepare(DateTimeOffset.UtcNow);

                ViewModel.ToggleTagSuggestionsCommand.Execute();

                ViewModel.IsSuggestingTags.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void AddsHashtagSymbolAtTheEndOfTheQueryInOrderToTagSuggestionMode()
            {
                const string description = "Testing Toggl Apps";
                var expected = $"{description}#";
                ViewModel.Prepare(DateTimeOffset.UtcNow);
                ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, description.Length);

                ViewModel.ToggleTagSuggestionsCommand.Execute();

                ViewModel.TextFieldInfo.Text.Should().Be(expected);
            }

            [Theory, LogIfTooSlow]
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

            [Theory, LogIfTooSlow]
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
            [Fact, LogIfTooSlow]
            public async Task SetsTheStartDateToTheValueReturnedByTheSelectDateTimeDialogViewModel()
            {
                var now = DateTimeOffset.UtcNow;
                var parameterToReturn = DurationParameter.WithStartAndDuration(now.AddHours(-2), null);
                NavigationService
                    .Navigate<DurationParameter, DurationParameter>(typeof(EditDurationViewModel), Arg.Any<DurationParameter>())
                    .Returns(parameterToReturn);
                ViewModel.Prepare(now);

                await ViewModel.ChangeDurationCommand.ExecuteAsync();

                ViewModel.StartTime.Should().Be(parameterToReturn.Start);
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheIsEditingDurationDateToTrueWhileTheViewDoesNotReturnAndThenSetsItBackToFalse()
            {
                var now = DateTimeOffset.UtcNow;
                var parameterToReturn = DurationParameter.WithStartAndDuration(now.AddHours(-2), null);
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

                [Fact, LogIfTooSlow]
                public async Task WithTheDatePassedWhenNavigatingToTheViewModel()
                {
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, 0);

                    ViewModel.DoneCommand.Execute();

                    await DataSource.TimeEntries.Received().Start(Arg.Is<StartTimeEntryDTO>(dto =>
                        dto.StartTime == startDate
                    ));
                }

                [Theory, LogIfTooSlow]
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

                [Fact, LogIfTooSlow]
                public async Task WithTheDefaultWorkspaceIfNoProjectIsProvided()
                {
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, 0);

                    ViewModel.DoneCommand.Execute();

                    await DataSource.TimeEntries.Received().Start(Arg.Is<StartTimeEntryDTO>(dto =>
                        dto.WorkspaceId == defaultWorkspaceId
                    ));
                }

                [Fact, LogIfTooSlow]
                public async Task WithTheProjectWorkspaceIfAProjectIsProvided()
                {
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty
                        .WithTextAndCursor(description, 0)
                        .WithProjectInfo(projectWorkspaceId, projectId, "Something", "#123123");

                    ViewModel.DoneCommand.Execute();

                    await DataSource.TimeEntries.Received().Start(Arg.Is<StartTimeEntryDTO>(dto =>
                        dto.WorkspaceId == projectWorkspaceId
                    ));
                }

                [Fact, LogIfTooSlow]
                public async Task WithTheAppropriateWorkspaceSelectedIfNoProjectWasTapped()
                {
                    const long expectedWorkspace = 1234;
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty
                        .WithTextAndCursor(description, 0)
                        .WithProjectInfo(projectWorkspaceId, projectId, "Something", "#123123");
                    ViewModel.SelectSuggestionCommand
                             .Execute(ProjectSuggestion.NoProject(expectedWorkspace, ""));

                    ViewModel.DoneCommand.Execute();

                    await DataSource.TimeEntries.Received().Start(Arg.Is<StartTimeEntryDTO>(dto =>
                        dto.WorkspaceId == expectedWorkspace
                    ));
                }

                [Fact, LogIfTooSlow]
                public async Task WithTheCurrentUsersId()
                {
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, 0);
                    ViewModel.DoneCommand.Execute();

                    await DataSource.TimeEntries.Received().Start(Arg.Is<StartTimeEntryDTO>(dto =>
                        dto.UserId == userId
                    ));
                }

                [Fact, LogIfTooSlow]
                public async Task WithTheAppropriateProjectId()
                {
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty
                        .WithTextAndCursor(description, 0)
                        .WithProjectInfo(projectWorkspaceId, projectId, "Something", "#123123");

                    ViewModel.DoneCommand.Execute();

                    await DataSource.TimeEntries.Received().Start(Arg.Is<StartTimeEntryDTO>(dto =>
                        dto.ProjectId == projectId
                    ));
                }

                [Fact, LogIfTooSlow]
                public async Task WithTheAppropriateDescription()
                {
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, 0);

                    ViewModel.DoneCommand.Execute();

                    await DataSource.TimeEntries.Received().Start(Arg.Is<StartTimeEntryDTO>(dto =>
                        dto.Description == description
                    ));
                }

                [Fact, LogIfTooSlow]
                public async Task WithTheAppropriateTaskId()
                {
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty
                        .WithTextAndCursor(description, 0)
                        .WithProjectAndTaskInfo(projectWorkspaceId, projectId, "Something", "#AABBCC", taskId, "Some task");

                    ViewModel.DoneCommand.Execute();

                    await DataSource.TimeEntries.Received().Start(Arg.Is<StartTimeEntryDTO>(dto =>
                        dto.TaskId == taskId
                    ));
                }

                [Fact, LogIfTooSlow]
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

                [Fact, LogIfTooSlow]
                public async Task InitiatesPushSync()
                {
                    ViewModel.DoneCommand.Execute();

                    await DataSource.SyncManager.Received().PushSync();
                }

                [Fact, LogIfTooSlow]
                public async Task DoesNotInitiatePushSyncWhenSavingFails()
                {
                    DataSource.TimeEntries.Start(Arg.Any<StartTimeEntryDTO>())
                        .Returns(Observable.Throw<IDatabaseTimeEntry>(new Exception()));

                    ViewModel.DoneCommand.Execute();

                    await DataSource.SyncManager.DidNotReceive().PushSync();
                }

                [Theory, LogIfTooSlow]
                [InlineData(null)]
                [InlineData(" ")]
                [InlineData("\t")]
                [InlineData("\n")]
                [InlineData("               ")]
                [InlineData("      \t  \n     ")]
                public async Task ReducesDescriptionConsistingOfOnlyEmptyCharactersToAnEmptyString(string description)
                {
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, 0);

                    ViewModel.DoneCommand.Execute();

                    await DataSource.TimeEntries.Received().Start(Arg.Is<StartTimeEntryDTO>(dto =>
                        dto.Description.Length == 0
                    ));
                }

                [Theory, LogIfTooSlow]
                [InlineData("   abcde", "abcde")]
                [InlineData("abcde     ", "abcde")]
                [InlineData("  abcde ", "abcde")]
                [InlineData("abcde  fgh", "abcde  fgh")]
                [InlineData("      abcd\nefgh     ", "abcd\nefgh")]
                public async Task TrimsDescriptionFromTheStartAndTheEndBeforeSaving(string description, string trimmed)
                {
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, description.Length);

                    ViewModel.DoneCommand.Execute();

                    await DataSource.TimeEntries.Received().Start(Arg.Is<StartTimeEntryDTO>(dto =>
                        dto.Description == trimmed
                    ));
                }

                private TagSuggestion tagSuggestionFromInt(int i)
                {
                    var tag = Substitute.For<IDatabaseTag>();
                    tag.Id.Returns(i);
                    tag.Name.Returns(i.ToString());

                    return new TagSuggestion(tag);
                }
            }

            [Fact, LogIfTooSlow]
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
                [Fact, LogIfTooSlow]
                public void SetsTheProjectIdToTheSuggestedProjectId()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.ProjectId.Should().Be(ProjectId);
                }

                [Fact, LogIfTooSlow]
                public void SetsTheProjectNameToTheSuggestedProjectName()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.ProjectName.Should().Be(ProjectName);
                }

                [Fact, LogIfTooSlow]
                public void SetsTheProjectColorToTheSuggestedProjectColor()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.ProjectColor.Should().Be(ProjectColor);
                }

                [Theory, LogIfTooSlow]
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

                [Theory, LogIfTooSlow]
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

                [Fact, LogIfTooSlow]
                public void RemovesTheProjectQueryFromTheTextFieldInfo()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.Text.Should().Be("Something ");
                }

                [Fact, LogIfTooSlow]
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

                [Fact, LogIfTooSlow]
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

                [Fact, LogIfTooSlow]
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

                [Fact, LogIfTooSlow]
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

                [Fact, LogIfTooSlow]
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

                [Fact, LogIfTooSlow]
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

                [Fact, LogIfTooSlow]
                public void RemovesTheTagQueryFromTheTextFieldInfo()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.Text.Should().Be("Something ");
                }

                [Fact, LogIfTooSlow]
                public void AddsTheSuggestedTagToTheList()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.Tags.Should().Contain(Suggestion);
                }
            }

            public sealed class WhenSelectingAQuerySymbolSuggestion : SelectSuggestionTest<QuerySymbolSuggestion>
            {
                protected override QuerySymbolSuggestion Suggestion { get; } = QuerySymbolSuggestion.Suggestions.First();

                [Fact, LogIfTooSlow]
                public void SetsTheTextToTheQuerySymbolSelected()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.Text.Should().Be(Suggestion.Symbol);
                }
            }
        }

        public sealed class TheSuggestionsProperty : StartTimeEntryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void IsClearedWhenThereAreNoWordsToQuery()
            {
                ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor("", 0);

                ViewModel.Suggestions.Should().HaveCount(0);
            }
        }

        public sealed class TheDescriptionRemainingBytesProperty : StartTimeEntryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void IsMaxIfTheTextIsEmpty()
            {
                ViewModel.TextFieldInfo = TextFieldInfo.Empty;

                ViewModel.DescriptionRemainingBytes.Should()
                    .Be(MaxTimeEntryDescriptionLengthInBytes);
            }

            [Theory, LogIfTooSlow]
            [InlineData("Hello fox")]
            [InlineData("Some emojis: 🔥😳👻")]
            [InlineData("Some weird characters: āčēļķīņš")]
            [InlineData("Some random arabic characters: ظۓڰڿڀ")]
            public void IsDecreasedForEachByteInTheText(string text)
            {
                var expectedRemainingByteCount
                    = MaxTimeEntryDescriptionLengthInBytes - text.LengthInBytes();

                ViewModel.TextFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(text, 0);

                ViewModel.DescriptionRemainingBytes.Should()
                    .Be(expectedRemainingByteCount);
            }

            [Fact, LogIfTooSlow]
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
            [Theory, LogIfTooSlow]
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

            [Theory, LogIfTooSlow]
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
