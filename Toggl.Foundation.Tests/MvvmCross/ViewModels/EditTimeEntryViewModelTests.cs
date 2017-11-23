using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Models;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.PrimeRadiant.Models;
using Xunit;
using static Toggl.Foundation.Helper.Constants;
using static Toggl.Multivac.Extensions.StringExtensions;
using Task = System.Threading.Tasks.Task;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class EditTimeEntryViewModelTests
    {
        public abstract class EditTimeEntryViewModelTest : BaseViewModelTests<EditTimeEntryViewModel>
        {
            protected const long Id = 10;

            protected void ConfigureEditedTimeEntry(DateTimeOffset now, bool isRunning = false)
            {
                var te = TimeEntry.Builder.Create(Id)
                    .SetDescription("Something")
                    .SetStart(now.AddHours(-2))
                    .SetAt(now.AddHours(-2))
                    .SetWorkspaceId(11)
                    .SetUserId(12);

                if (!isRunning)
                    te = te.SetDuration((long)TimeSpan.FromHours(1).TotalSeconds);

                var observable = Observable.Return(te.Build());

                DataSource.TimeEntries.GetById(Arg.Is(Id)).Returns(observable);
            }

            protected override EditTimeEntryViewModel CreateViewModel()
                => new EditTimeEntryViewModel(DataSource, NavigationService, TimeService, DialogService);
        }

        public sealed class TheConstructor : EditTimeEntryViewModelTest
        {
            [Theory]
            [ClassData(typeof(FourParameterConstructorTestData))]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useDataSource, bool useNavigationService, bool useTimeService, bool useDialogService)
            {
                var dataSource = useDataSource ? DataSource : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var timeService = useTimeService ? TimeService : null;
                var dialogService = useDialogService ? DialogService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new EditTimeEntryViewModel(dataSource, navigationService, timeService, dialogService);

                tryingToConstructWithEmptyParameters.ShouldThrow<ArgumentNullException>();
            }
        }

        public sealed class TheCloseCommand : EditTimeEntryViewModelTest
        {
            [Fact]
            public async Task ClosesTheViewModel()
            {
                await ViewModel.CloseCommand.ExecuteAsync();

                await NavigationService.Received().Close(Arg.Is(ViewModel));
            }
        }

        public class TheDeleteCommand : EditTimeEntryViewModelTest
        {
            protected void PrepareActionSheet(bool confirm)
            {
                var result = confirm ? Resources.Delete : Resources.Cancel;

                DialogService.ShowMultipleChoiceDialog(
                    Arg.Is(Resources.Cancel),
                    Arg.Is<MultipleChoiceDialogAction>(
                        action => action.Text == Resources.Delete
                               && action.Destructive == true)
                ).Returns(Task.FromResult(result));
            }

            [Fact]
            public async Task ShowsConfirmationActionSheet()
            {
                await ViewModel.DeleteCommand.ExecuteAsync();

                await DialogService.Received().ShowMultipleChoiceDialog(
                    Arg.Is(Resources.Cancel),
                    Arg.Is<MultipleChoiceDialogAction>(
                        action => action.Text == Resources.Delete
                               && action.Destructive == true)
                );
            }

            public sealed class WhenUserConfirms : TheDeleteCommand
            {
                public WhenUserConfirms()
                {
                    PrepareActionSheet(true);
                }

                [Fact]
                public async Task CallsDeleteOnDataSource()
                {
                    await ViewModel.DeleteCommand.ExecuteAsync();

                    await DataSource.TimeEntries.Received().Delete(Arg.Is(ViewModel.Id));
                }

                [Fact]
                public async Task InitiatesPushSync()
                {
                    await ViewModel.DeleteCommand.ExecuteAsync();

                    await DataSource.SyncManager.Received().PushSync();
                }

                [Fact]
                public async Task DoesNotInitiatePushSyncWhenDeletingFails()
                {
                    DataSource.TimeEntries.Delete(Arg.Any<long>())
                        .Returns(Observable.Throw<Unit>(new Exception()));

                    await ViewModel.DeleteCommand.ExecuteAsync();

                    await DataSource.SyncManager.DidNotReceive().PushSync();
                }
            }

            public sealed class WhenUserCancels : TheDeleteCommand
            {
                public WhenUserCancels()
                {
                    PrepareActionSheet(false);
                }

                [Fact]
                public async Task DoesNotCallDeleteOnDataSource()
                {
                    await ViewModel.DeleteCommand.ExecuteAsync();

                    await DataSource.TimeEntries.DidNotReceive().Delete(Arg.Is(ViewModel.Id));
                }

                [Fact]
                public async Task DoesNotInitiatePushSync()
                {
                    await ViewModel.DeleteCommand.ExecuteAsync();

                    await DataSource.SyncManager.DidNotReceive().PushSync();
                }
            }
        }

        public sealed class TheSelectStartDateTimeCommandCommand : EditTimeEntryViewModelTest
        {
            [Property]
            public void CallsTheSelectDateTimeViewModelWithAMaxDateEqualToTheCurrentTimeWhenTheTimeEntryIsRunning(DateTimeOffset now)
            {
                if (DateTimeOffset.MinValue.AddHours(MaxTimeEntryDurationInHours) <= now ||
                    DateTimeOffset.MaxValue.AddHours(-1) >= now) return;

                TimeService.CurrentDateTime.Returns(now);

                var parameterToReturn = now.AddHours(-2);
                NavigationService
                    .Navigate<DatePickerParameters, DateTimeOffset>(typeof(SelectDateTimeViewModel), Arg.Any<DatePickerParameters>())
                    .Returns(parameterToReturn);
                ConfigureEditedTimeEntry(now, true);
                ViewModel.Prepare(Id);

                ViewModel.SelectStartDateTimeCommand.ExecuteAsync().Wait();

                NavigationService
                    .Received()
                    .Navigate<DatePickerParameters, DateTimeOffset>(
                        typeof(SelectDateTimeViewModel),
                        Arg.Is<DatePickerParameters>(p => p.MaxDate == now));
            }

            [Property]
            public void CallsTheSelectDateTimeViewModelWithAMaxDateEqualToTheValueOfTheStopTimeIfTheTimeEntryIsComplete(DateTimeOffset now)
            {
                if (DateTimeOffset.MinValue.AddHours(MaxTimeEntryDurationInHours) <= now ||
                    DateTimeOffset.MaxValue.AddHours(-1) >= now) return;

                TimeService.CurrentDateTime.Returns(now);

                var parameterToReturn = now.AddHours(-2);
                NavigationService
                    .Navigate<DatePickerParameters, DateTimeOffset>(typeof(SelectDateTimeViewModel), Arg.Any<DatePickerParameters>())
                    .Returns(parameterToReturn);
                ConfigureEditedTimeEntry(now);
                ViewModel.Prepare(Id);

                ViewModel.SelectStartDateTimeCommand.ExecuteAsync().Wait();

                NavigationService
                    .Received()
                    .Navigate<DatePickerParameters, DateTimeOffset>(
                        typeof(SelectDateTimeViewModel),
                        Arg.Is<DatePickerParameters>(p => p.MinDate == now.AddHours(-2)));
            }

            [Property]
            public void CallsTheSelectDateTimeViewModelWithAMinDateThatAllows999HoursOfDuration(DateTimeOffset now)
            {
                if (DateTimeOffset.MinValue.AddHours(MaxTimeEntryDurationInHours) <= now ||
                    DateTimeOffset.MaxValue.AddHours(-1) >= now) return;

                TimeService.CurrentDateTime.Returns(now);

                var parameterToReturn = now.AddHours(-2);
                NavigationService
                    .Navigate<DatePickerParameters, DateTimeOffset>(typeof(SelectDateTimeViewModel), Arg.Any<DatePickerParameters>())
                    .Returns(parameterToReturn);
                ConfigureEditedTimeEntry(now);
                ViewModel.Prepare(Id);

                ViewModel.SelectStartDateTimeCommand.ExecuteAsync().Wait();

                NavigationService
                    .Received()
                    .Navigate<DatePickerParameters, DateTimeOffset>(
                        typeof(SelectDateTimeViewModel),
                        Arg.Is<DatePickerParameters>(p => p.MaxDate == p.MaxDate.AddHours(-MaxTimeEntryDurationInHours)));
            }

            [Property]
            public void SetsTheStartDateToTheValueReturnedByTheSelectDateTimeDialogViewModel(DateTimeOffset now)
            {
                if (DateTimeOffset.MinValue.AddHours(MaxTimeEntryDurationInHours) <= now ||
                    DateTimeOffset.MaxValue.AddHours(-1) >= now) return;

                var parameterToReturn = now.AddHours(-2);
                NavigationService
                    .Navigate<DatePickerParameters, DateTimeOffset>(typeof(SelectDateTimeViewModel), Arg.Any<DatePickerParameters>())
                    .Returns(parameterToReturn);
                ConfigureEditedTimeEntry(now);
                ViewModel.Prepare(Id);

                ViewModel.SelectStartDateTimeCommand.ExecuteAsync().Wait();

                ViewModel.StartTime.Should().Be(parameterToReturn);
            }
        }

        public sealed class TheEditDurationCommand : EditTimeEntryViewModelTest
        {
            [Property]
            public void SetsTheStartTimeToTheValueReturnedByTheSelectDateTimeDialogViewModelWhenEditingARunningTimeEntry(DateTimeOffset now)
            {
                var parameterToReturn = DurationParameter.WithStartAndStop(now.AddHours(-3), null);
                NavigationService
                    .Navigate<DurationParameter, DurationParameter>(typeof(EditDurationViewModel), Arg.Any<DurationParameter>())
                    .Returns(parameterToReturn);
                ConfigureEditedTimeEntry(now);
                ViewModel.Prepare(Id);

                ViewModel.EditDurationCommand.ExecuteAsync().Wait();

                ViewModel.StartTime.Should().Be(parameterToReturn.Start);
            }

            [Property]
            public void SetsTheStopTimeToTheValueReturnedByTheSelectDateTimeDialogViewModelWhenEditingACompletedTimeEntry(DateTimeOffset now)
            {
                var parameterToReturn = DurationParameter.WithStartAndStop(now.AddHours(-4), now.AddHours(-3));
                NavigationService
                    .Navigate<DurationParameter, DurationParameter>(typeof(EditDurationViewModel), Arg.Any<DurationParameter>())
                    .Returns(parameterToReturn);
                ConfigureEditedTimeEntry(now);
                ViewModel.Prepare(Id);

                ViewModel.EditDurationCommand.ExecuteAsync().Wait();

                ViewModel.StopTime.Should().Be(parameterToReturn.Stop);
            }
        }

        public sealed class TheConfirmCommand : EditTimeEntryViewModelTest
        {
            [Fact]
            public async Task InitiatesPushSync()
            {
                ViewModel.ConfirmCommand.Execute();

                await DataSource.SyncManager.Received().PushSync();
            }

            [Fact]
            public async Task DoesNotInitiatePushSyncWhenSavingFails()
            {
                DataSource.TimeEntries.Update(Arg.Any<EditTimeEntryDto>())
                    .Returns(Observable.Throw<IDatabaseTimeEntry>(new Exception()));

                ViewModel.ConfirmCommand.Execute();

                await DataSource.SyncManager.DidNotReceive().PushSync();
            }

            [Fact]
            public async Task UpdatesWorkspaceIdIfProjectFromAnotherWorkspaceWasSelected()
            {
                var timeEntry = Substitute.For<IDatabaseTimeEntry>();
                timeEntry.Id.Returns(10);
                timeEntry.WorkspaceId.Returns(11);
                timeEntry.ProjectId.Returns(12);
                DataSource.TimeEntries.GetById(Arg.Is(timeEntry.Id))
                  .Returns(Observable.Return(timeEntry));
                var newProjectId = 20;
                var project = Substitute.For<IDatabaseProject>();
                project.Id.Returns(newProjectId);
                project.WorkspaceId.Returns(21);
                DataSource.Projects.GetById(project.Id)
                    .Returns(Observable.Return(project));
                ViewModel.Prepare(timeEntry.Id);
                await ViewModel.Initialize();
                var parameter = SelectProjectParameter.WithIds(newProjectId, null, project.WorkspaceId);
                NavigationService.Navigate<SelectProjectParameter, SelectProjectParameter>(
                        typeof(SelectProjectViewModel),
                        Arg.Any<SelectProjectParameter>())
                    .Returns(parameter);
                await ViewModel.SelectProjectCommand.ExecuteAsync();

                ViewModel.ConfirmCommand.Execute();

                await DataSource.TimeEntries.Received().Update(
                    Arg.Is<EditTimeEntryDto>(dto => dto.WorkspaceId == project.WorkspaceId));
            }

            [Fact]
            public async Task DoesNotUpdateWorkspaceIdIfProjectFromTheSameWorkspaceIsSelected()
            {
                var workspaceId = 11;
                var timeEntry = Substitute.For<IDatabaseTimeEntry>();
                timeEntry.Id.Returns(10);
                timeEntry.WorkspaceId.Returns(workspaceId);
                timeEntry.ProjectId.Returns(12);
                DataSource.TimeEntries.GetById(Arg.Is(timeEntry.Id))
                  .Returns(Observable.Return(timeEntry));
                var newProjectId = 20;
                var project = Substitute.For<IDatabaseProject>();
                project.Id.Returns(newProjectId);
                project.WorkspaceId.Returns(workspaceId);
                DataSource.Projects.GetById(project.Id)
                    .Returns(Observable.Return(project));
                ViewModel.Prepare(timeEntry.Id);
                await ViewModel.Initialize();
                NavigationService.Navigate<SelectProjectParameter, SelectProjectParameter>(
                        typeof(SelectProjectViewModel),
                        Arg.Any<SelectProjectParameter>())
                    .Returns(SelectProjectParameter.WithIds(newProjectId, null, workspaceId));
                await ViewModel.SelectProjectCommand.ExecuteAsync();

                ViewModel.ConfirmCommand.Execute();

                await DataSource.TimeEntries.Received().Update(
                    Arg.Is<EditTimeEntryDto>(dto => dto.WorkspaceId == workspaceId));
            }

            [Fact]
            public async Task UpdatewWorkspaceIdIfNoProjectWasSelected()
            {
                var oldWorkspaceId = 11;
                var newWorkspaceId = 21;
                var timeEntry = Substitute.For<IDatabaseTimeEntry>();
                timeEntry.Id.Returns(10);
                timeEntry.WorkspaceId.Returns(oldWorkspaceId);
                timeEntry.ProjectId.Returns(12);
                DataSource.TimeEntries.GetById(Arg.Is(timeEntry.Id))
                  .Returns(Observable.Return(timeEntry));
                ViewModel.Prepare(timeEntry.Id);
                await ViewModel.Initialize();
                NavigationService.Navigate<SelectProjectParameter, SelectProjectParameter>(
                        typeof(SelectProjectViewModel),
                        Arg.Any<SelectProjectParameter>())
                    .Returns(SelectProjectParameter.WithIds(null, null, newWorkspaceId));
                await ViewModel.SelectProjectCommand.ExecuteAsync();

                ViewModel.ConfirmCommand.Execute();

                await DataSource.TimeEntries.Received().Update(
                    Arg.Is<EditTimeEntryDto>(dto => dto.WorkspaceId == newWorkspaceId));
            }

            [Theory]
            [InlineData(null)]
            [InlineData(" ")]
            [InlineData("\t")]
            [InlineData("\n")]
            [InlineData("               ")]
            [InlineData("      \t  \n     ")]
            public async Task ReducesDescriptionConsistingOfOnlyEmptyCharactersToAnEmptyString(string description)
            {
                ViewModel.Description = description;

                ViewModel.ConfirmCommand.Execute();

                await DataSource.TimeEntries.Received().Update(Arg.Is<EditTimeEntryDto>(dto =>
                    dto.Description.Length == 0
                ));
            }

            [Theory]
            [InlineData(null, "")]
            [InlineData("   abcde", "abcde")]
            [InlineData("abcde     ", "abcde")]
            [InlineData("  abcde ", "abcde")]
            [InlineData("abcde  fgh", "abcde  fgh")]
            [InlineData("      abcd\nefgh     ", "abcd\nefgh")]
            public async Task TrimsDescriptionFromTheStartAndTheEndBeforeSaving(string description, string trimmed)
            {
                ViewModel.Description = description;

                ViewModel.ConfirmCommand.Execute();

                await DataSource.TimeEntries.Received().Update(Arg.Is<EditTimeEntryDto>(dto =>
                    dto.Description == trimmed
                ));
            }
        }

        public sealed class TheSelectTagsCommand : EditTimeEntryViewModelTest
        {
            [Property]
            public void NavigatesToTheSelectTagsViewModelPassingCurrentTagIds(NonNegativeInt[] nonNegativeInts)
            {
                var tagIds = nonNegativeInts.Select(i => (long)i.Get)
                    .Distinct();
                long id = 13;
                var timeEntry = Substitute.For<IDatabaseTimeEntry>();
                timeEntry.Id.Returns(id);
                timeEntry.TagIds.Returns(tagIds);
                DataSource.TimeEntries.GetById(Arg.Is(id)).Returns(Observable.Return(timeEntry));
                ViewModel.Prepare(id);
                ViewModel.Initialize().Wait();

                ViewModel.SelectTagsCommand.ExecuteAsync().Wait();

                NavigationService
                    .Received()
                    .Navigate<(long[] tagIds, long workspaceId), long[]>(
                        Arg.Is(typeof(SelectTagsViewModel)),
                        Arg.Is<(long[] tagIds, long workspaceId)>(
                            tuple => tuple.tagIds.SequenceEqual(tagIds)))
                    .Wait();
            }

            [Fact]
            public async Task NavigatesToTheSelectTagsViewModelPassingWorkspaceId()
            {
                long workspaceId = 13;
                var workspace = Substitute.For<IDatabaseWorkspace>();
                workspace.Id.Returns(workspaceId);
                var timeEntry = Substitute.For<IDatabaseTimeEntry>();
                timeEntry.Id.Returns(14);
                timeEntry.WorkspaceId.Returns(workspaceId);
                DataSource.TimeEntries.GetById(Arg.Any<long>())
                    .Returns(Observable.Return(timeEntry));
                ViewModel.Prepare(timeEntry.Id);
                await ViewModel.Initialize();

                await ViewModel.SelectTagsCommand.ExecuteAsync();

                await NavigationService
                    .Received()
                    .Navigate<(long[] tagIds, long workspaceId), long[]>(
                        Arg.Is(typeof(SelectTagsViewModel)),
                        Arg.Is<(long[] tagIds, long workspaceId)>(
                            tuple => tuple.workspaceId == workspaceId)
                    );
            }

            [Property]
            public void QueriesTheDataSourceForReturnedTagIds(
                NonEmptyArray<NonNegativeInt> nonNegativeInts)
            {
                var tagIds = nonNegativeInts.Get
                    .Select(i => (long)i.Get)
                    .ToArray();
                var tags = tagIds.Select(createTag);
                DataSource.Tags.GetAll(Arg.Any<Func<IDatabaseTag, bool>>())
                    .Returns(Observable.Return(tags));
                NavigationService
                    .Navigate<(long[], long), long[]>(Arg.Is(typeof(SelectTagsViewModel)), Arg.Any<(long[], long)>())
                    .Returns(Task.FromResult(tagIds));
                ViewModel.Initialize().Wait();

                ViewModel.SelectTagsCommand.ExecuteAsync().Wait();

                DataSource.Tags.Received()
                    .GetAll(Arg.Is<Func<IDatabaseTag, bool>>(
                        func => tags.All(func)))
                    .Wait();
            }

            [Property]
            public void SetsTheReturnedTags(NonEmptyArray<NonNegativeInt> nonNegativeInts)
            {
                var tagIds = nonNegativeInts.Get
                    .Select(i => (long)i.Get)
                    .ToArray();
                var tags = tagIds.Select(createTag);
                var tagNames = new HashSet<string>(tags.Select(tag => tag.Name));
                ViewModel.Initialize().Wait();
                DataSource.Tags.GetAll(Arg.Any<Func<IDatabaseTag, bool>>())
                    .Returns(Observable.Return(tags));
                NavigationService
                    .Navigate<(long[], long), long[]>(Arg.Is(typeof(SelectTagsViewModel)), Arg.Any<(long[], long)>())
                    .Returns(Task.FromResult(tagIds));

                ViewModel.SelectTagsCommand.ExecuteAsync().Wait();

                ViewModel.Tags.Should()
                         .HaveCount(tags.Count()).And
                         .OnlyContain(tag => tagNames.Contains(tag));
            }

            private IDatabaseTag createTag(long id)
            {
                var tag = Substitute.For<IDatabaseTag>();
                tag.Id.Returns(id);
                tag.Name.Returns($"Tag{id}");
                return tag;
            }
        }

        public sealed class TheDismissSyncErrorMessageCommand : EditTimeEntryViewModelTest
        {
            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public async Task SetsSyncErrorMessageVisiblePropertyToFalse(bool initialValue)
            {
                var errorMessage = initialValue ? "Some error" : null;
                var id = 13;
                var timeEntry = Substitute.For<IDatabaseTimeEntry>();
                timeEntry.Id.Returns(id);
                timeEntry.LastSyncErrorMessage.Returns(errorMessage);
                DataSource.TimeEntries.GetById(Arg.Is<long>(id)).Returns(Observable.Return(timeEntry));
                ViewModel.Prepare(id);
                await ViewModel.Initialize();

                ViewModel.DismissSyncErrorMessageCommand.Execute();

                ViewModel.SyncErrorMessageVisible.Should().BeFalse();
            }
        }

        public sealed class TheInitializeMethod : EditTimeEntryViewModelTest
        {
            private readonly IDatabaseTimeEntry timeEntry;

            public TheInitializeMethod()
            {
                timeEntry = Substitute.For<IDatabaseTimeEntry>();
                timeEntry.Id.Returns(Id);
                DataSource.TimeEntries.GetById(Arg.Is<long>(Id)).Returns(Observable.Return(timeEntry));
            }

            [Property]
            public void SetsTheSyncErrorMessageProperty(string errorMessage)
            {
                timeEntry.LastSyncErrorMessage.Returns(errorMessage);
                ViewModel.Prepare(Id);

                ViewModel.Initialize().Wait();

                ViewModel.SyncErrorMessage.Should().Be(errorMessage);
            }

            [Theory]
            [InlineData("Some error", true)]
            [InlineData("", false)]
            [InlineData(null, false)]
            public async Task SetsTheSyncErrorMessageVisibleProperty(
                string errorMessage, bool expectedVisibility)
            {
                timeEntry.LastSyncErrorMessage.Returns(errorMessage);
                ViewModel.Prepare(Id);

                await ViewModel.Initialize();

                ViewModel.SyncErrorMessageVisible.Should().Be(expectedVisibility);
            }
        }

        public sealed class TheSelectProjectCommand : EditTimeEntryViewModelTest
        {
            private async Task prepare(
                long? projectId = null,
                string projectName = null,
                string projectColor = null,
                string clientName = null,
                long? taskId = null,
                string taskName = null)
            {
                long timeEntryId = 10;
                prepareTimeEntry(timeEntryId);

                if (projectId.HasValue)
                    prepareProject(projectId.Value, projectName, projectColor, clientName, 0);

                if (taskId.HasValue)
                    prepareTask(taskId.Value, taskName);

                prepareNavigationService(projectId, taskId);

                ViewModel.Prepare(timeEntryId);
                await ViewModel.Initialize();
            }

            private IDatabaseTimeEntry prepareTimeEntry(long id)
            {
                var timeEntry = Substitute.For<IDatabaseTimeEntry>();
                timeEntry.Id.Returns(id);
                timeEntry.Description.Returns("Fuckface");
                timeEntry.Project.Name.Returns(Guid.NewGuid().ToString());
                timeEntry.Project.Color.Returns(Guid.NewGuid().ToString());
                timeEntry.Task.Name.Returns(Guid.NewGuid().ToString());
                timeEntry.Project.Client.Name.Returns(Guid.NewGuid().ToString());
                DataSource.TimeEntries.GetById(Arg.Is(id))
                    .Returns(Observable.Return(timeEntry));
                return timeEntry;
            }

            private IDatabaseProject prepareProject(
                long projectId, string projectName, string projectColor, string clientName, long workspaceId)
            {
                var project = Substitute.For<IDatabaseProject>();
                project.Id.Returns(projectId);
                project.Name.Returns(projectName);
                project.Color.Returns(projectColor);
                project.Client.Name.Returns(clientName);
                project.WorkspaceId.Returns(workspaceId);
                DataSource.Projects.GetById(Arg.Is(projectId))
                    .Returns(Observable.Return(project));
                return project;
            }

            private void prepareTask(long taskId, string taskName)
            {
                var task = Substitute.For<IDatabaseTask>();
                task.Id.Returns(taskId);
                task.Name.Returns(taskName);
                DataSource.Tasks.GetById(Arg.Is(task.Id))
                    .Returns(Observable.Return(task));
            }

            private void prepareNavigationService(long? projectId, long? taskId)
                => NavigationService
                       .Navigate<SelectProjectParameter, SelectProjectParameter>(
                           typeof(SelectProjectViewModel),
                           Arg.Any<SelectProjectParameter>())
                       .Returns(SelectProjectParameter.WithIds(projectId, taskId, 0));

            private List<IDatabaseTag> createTags(int count)
                => Enumerable.Range(10000, count)
                    .Select(i =>
                    {
                        var tag = Substitute.For<IDatabaseTag>();
                        tag.Name.Returns($"Tag{i}");
                        tag.Id.Returns(i);
                        return tag;
                    }).ToList();

            [Fact]
            public async Task SetsTheProject()
            {
                var projectName = "Some other project";
                await prepare(projectId: 11, projectName: projectName);

                await ViewModel.SelectProjectCommand.ExecuteAsync();

                ViewModel.Project.Should().Be(projectName);
            }

            [Fact]
            public async Task SetsTheTask()
            {
                var taskName = "Some task";
                await prepare(
                    projectId: 11,
                    projectName: "Project",
                    taskId: 12,
                    taskName: taskName);

                await ViewModel.SelectProjectCommand.ExecuteAsync();

                ViewModel.Task.Should().Be(taskName);
            }

            [Fact]
            public async Task SetsTheClient()
            {
                var clientName = "Some client";
                await prepare(
                    projectId: 11,
                    projectName: "Project",
                    clientName: clientName);

                await ViewModel.SelectProjectCommand.ExecuteAsync();

                ViewModel.Client.Should().Be(clientName);
            }

            [Fact]
            public async Task SetsTheColor()
            {
                var projectColor = "123456";
                await prepare(
                    projectId: 11,
                    projectName: "Project",
                    projectColor: projectColor);

                await ViewModel.SelectProjectCommand.ExecuteAsync();

                ViewModel.ProjectColor.Should().Be(projectColor);
            }

            [Fact]
            public async Task RemovesTheTaskIfNoTaskWasSelected()
            {
                await prepare(11, "Some project");

                await ViewModel.SelectProjectCommand.ExecuteAsync();

                ViewModel.Task.Should().BeEmpty();
            }

            [Fact]
            public async Task RemovesTagsIfProjectFromAnotherWorkspaceWasSelected()
            {
                var initialTagCount = 10;
                long timeEntryId = 10;
                long initialProjectId = 11;
                long newProjectId = 12;
                prepareProject(initialProjectId, "Initial project", "#123456", "Some client", 13);
                prepareProject(newProjectId, "New project", "AABBCC", "Some client", 14);
                prepareNavigationService(newProjectId, null);
                var timeEntry = prepareTimeEntry(timeEntryId);
                var tags = createTags(initialTagCount);
                timeEntry.Tags.Returns(tags);
                ViewModel.Prepare(timeEntryId);
                await ViewModel.Initialize();
                ViewModel.Tags.Should().HaveCount(initialTagCount);

                await ViewModel.SelectProjectCommand.ExecuteAsync();

                ViewModel.Tags.Should().HaveCount(0);
            }
        }

        public sealed class TheTagsProperty : EditTimeEntryViewModelTest
        {
            [Theory]
            [InlineData(31, "a")]
            [InlineData(31, "💵")]
            [InlineData(50, "b")]
            [InlineData(50, "🚕")]
            public async Task CutsLongTagNames(int tagLength, string tagGrapheme)
            {
                await prepareTest(tagLength, tagGrapheme);

                ViewModel.Tags.Should()
                    .OnlyContain(tag => tag.LengthInGraphemes() == 33 && tag.EndsWith("..."));
            }

            [Theory]
            [InlineData(30, "a")]
            [InlineData(30, "🐕")]
            [InlineData(29, "b")]
            [InlineData(29, "🐛")]
            [InlineData(10, "c")]
            [InlineData(10, "❄️")]
            public async Task DoesNotCutShortTagNames(int tagLength, string tagGrapheme)
            {
                await prepareTest(tagLength, tagGrapheme);

                ViewModel.Tags.Should().OnlyContain(tag => tag.LengthInGraphemes() == tagLength);
            }

            private async Task prepareTest(int tagLength, string tagGrapheme)
            {
                var tag = Substitute.For<IDatabaseTag>();
                tag.Name.Returns(getLongTagName(tagLength, tagGrapheme));
                var timeEntry = Substitute.For<IDatabaseTimeEntry>();

                timeEntry.Id.Returns(13);
                timeEntry.Tags.Returns(new IDatabaseTag[] { tag });

                DataSource.TimeEntries.GetById(Arg.Is(timeEntry.Id))
                    .Returns(Observable.Return(timeEntry));
                
                ViewModel.Prepare(timeEntry.Id);
                await ViewModel.Initialize();
            }

            private string getLongTagName(int length, string tagGrapheme)
                => Enumerable
                    .Range(0, length)
                    .Aggregate(new StringBuilder(), (builder, _) => builder.Append(tagGrapheme))
                    .ToString();
        }

        public sealed class TheDescriptionLimitExceededProperty : EditTimeEntryViewModelTest
        {
            [Theory]
            [InlineData("a", MaxTimeEntryDescriptionLengthInBytes - 1)]
            [InlineData("c", MaxTimeEntryDescriptionLengthInBytes)]
            [InlineData("ॷ", MaxTimeEntryDescriptionLengthInBytes / 3)] //This symbol is 3 bytes long
            [InlineData("Љ", MaxTimeEntryDescriptionLengthInBytes / 2)] //This symbol is 2 bytes long
            public void IsFalseWhenDescriptionIsShorterThanMaxLimit(
                string character, int count)
            {
                ViewModel.Description = createLongString(character, count);

                ViewModel.DescriptionLimitExceeded.Should().BeFalse();
            }

            [Theory]
            [InlineData("A", MaxTimeEntryDescriptionLengthInBytes + 1)]
            [InlineData("ॷ", MaxTimeEntryDescriptionLengthInBytes / 3 + 1)] //This symbol is 3 bytes long
            [InlineData("Љ", MaxTimeEntryDescriptionLengthInBytes / 2 + 1)] //This symbol is 2 bytes long
            public void IsTrueWhenDescriptionIsLongerThanMaxLimit(
                string character, int count)
            {
                ViewModel.Description = createLongString(character, count);

                ViewModel.DescriptionLimitExceeded.Should().BeTrue();
            }

            private string createLongString(string character, int count)
                => Enumerable
                    .Range(0, count)
                    .Aggregate(
                        new StringBuilder(),
                        (builder, _) => builder.Append(character))
                    .ToString();
        }
    }
}
