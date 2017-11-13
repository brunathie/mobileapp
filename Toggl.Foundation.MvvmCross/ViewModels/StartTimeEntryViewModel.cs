using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using MvvmCross.Core.Navigation;
using MvvmCross.Core.ViewModels;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using static Toggl.Foundation.Helper.Constants;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class StartTimeEntryViewModel : MvxViewModel<DateTimeOffset>
    {
        //Fields
        private readonly ITimeService timeService;
        private readonly IDialogService dialogService;
        private readonly ITogglDataSource dataSource;
        private readonly IMvxNavigationService navigationService;
        private readonly Subject<TextFieldInfo> infoSubject = new Subject<TextFieldInfo>();
        private readonly Subject<AutocompleteSuggestionType> queryByTypeSubject = new Subject<AutocompleteSuggestionType>();

        private long workspaceId;
        private long? lastProjectId;
        private IDisposable queryDisposable;
        private IDisposable elapsedTimeDisposable;

        //Properties
        private int DescriptionByteCount
            => Encoding.UTF8.GetByteCount(TextFieldInfo.Text);

        public int DescriptionRemainingBytes
            => MaxTimeEntryDescriptionLengthInBytes - DescriptionByteCount;

        public bool DescriptionLengthExceeded
            => DescriptionByteCount > MaxTimeEntryDescriptionLengthInBytes;

        public bool UseGrouping { get; set; }

        public string CurrentQuery { get; private set; }

        public bool IsEditingDuration { get; private set; }

        public bool IsEditingStartDate { get; private set; }

        public bool IsSuggestingTags { get; private set; }

        public bool IsSuggestingProjects { get; private set; }

        public TextFieldInfo TextFieldInfo { get; set; } = TextFieldInfo.Empty;

        public TimeSpan ElapsedTime { get; private set; } = TimeSpan.Zero;

        public bool IsBillable { get; private set; } = false;

        public bool IsBillableAvailable { get; private set; } = false;

        public bool IsEditingProjects { get; private set; } = false;

        public bool IsEditingTags { get; private set; } = false;

        public DateTimeOffset StartTime { get; private set; }

        public DateTimeOffset? StopTime { get; private set; }

        public MvxObservableCollection<WorkspaceGroupedCollection<AutocompleteSuggestion>> Suggestions { get; }
            = new MvxObservableCollection<WorkspaceGroupedCollection<AutocompleteSuggestion>>();

        public IMvxAsyncCommand BackCommand { get; }

        public IMvxAsyncCommand DoneCommand { get; }

        public IMvxAsyncCommand ChangeDurationCommand { get; }

        public IMvxAsyncCommand ChangeStartTimeCommand { get; }

        public IMvxCommand ToggleBillableCommand { get; }

        public IMvxCommand ToggleTagSuggestionsCommand { get; }

        public IMvxCommand ToggleProjectSuggestionsCommand { get; }

        public IMvxAsyncCommand<string> CreateProjectCommand { get; }

        public IMvxAsyncCommand<AutocompleteSuggestion> SelectSuggestionCommand { get; }

        public IMvxCommand<ProjectSuggestion> ToggleTaskSuggestionsCommand { get; }

        public StartTimeEntryViewModel(
            ITimeService timeService,
            IDialogService dialogService,
            ITogglDataSource dataSource,
            IMvxNavigationService navigationService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(dialogService, nameof(dialogService));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));

            this.dataSource = dataSource;
            this.timeService = timeService;
            this.dialogService = dialogService;
            this.navigationService = navigationService;

            BackCommand = new MvxAsyncCommand(back);
            DoneCommand = new MvxAsyncCommand(done);
            ToggleBillableCommand = new MvxCommand(toggleBillable);
            ChangeDurationCommand = new MvxAsyncCommand(changeDuration);
            ChangeStartTimeCommand = new MvxAsyncCommand(changeStartTime);
            CreateProjectCommand = new MvxAsyncCommand<string>(createProject);
            ToggleTagSuggestionsCommand = new MvxCommand(toggleTagSuggestions);
            ToggleProjectSuggestionsCommand = new MvxCommand(toggleProjectSuggestions);
            SelectSuggestionCommand = new MvxAsyncCommand<AutocompleteSuggestion>(selectSuggestion);
            ToggleTaskSuggestionsCommand = new MvxCommand<ProjectSuggestion>(toggleTaskSuggestions);
        }

        private async Task selectSuggestion(AutocompleteSuggestion suggestion)
        {
            switch (suggestion)
            {
                case QuerySymbolSuggestion querySymbolSuggestion:
                    TextFieldInfo = TextFieldInfo.WithTextAndCursor(querySymbolSuggestion.Symbol, 1);
                    break;

                case TimeEntrySuggestion timeEntrySuggestion:

                    TextFieldInfo = TextFieldInfo.WithTextAndCursor(
                        timeEntrySuggestion.Description,
                        timeEntrySuggestion.Description.Length);

                    if (!timeEntrySuggestion.ProjectId.HasValue)
                    {
                        TextFieldInfo = TextFieldInfo.RemoveProjectInfo();
                        return;
                    }

                    if (timeEntrySuggestion.TaskId == null)
                    {
                        TextFieldInfo = TextFieldInfo.WithProjectInfo(
                            timeEntrySuggestion.ProjectId.Value,
                            timeEntrySuggestion.ProjectName,
                            timeEntrySuggestion.ProjectColor);
                        break;
                    }

                    TextFieldInfo = TextFieldInfo.WithProjectAndTaskInfo(
                        timeEntrySuggestion.ProjectId.Value,
                        timeEntrySuggestion.ProjectName,
                        timeEntrySuggestion.ProjectColor,
                        timeEntrySuggestion.TaskId.Value,
                        timeEntrySuggestion.TaskName);
                    break;

                case ProjectSuggestion projectSuggestion:

                    if (workspaceId == projectSuggestion.WorkspaceId)
                    {
                        setProject(projectSuggestion);
                        break;
                    }

                    var shouldChangeProject = await dialogService.Confirm(
                        Resources.DifferentWorkspaceAlertTitle,
                        Resources.DifferentWorkspaceAlertMessage,
                        Resources.Ok,
                        Resources.Cancel);

                    if (!shouldChangeProject) break;

                    setProject(projectSuggestion);

                    break;

                case TaskSuggestion taskSuggestion:

                    if (workspaceId == taskSuggestion.WorkspaceId)
                    {
                        setTask(taskSuggestion);
                        break;
                    }

                    var shouldChangeTask = await dialogService.Confirm(
                        Resources.DifferentWorkspaceAlertTitle,
                        Resources.DifferentWorkspaceAlertMessage,
                        Resources.Ok,
                        Resources.Cancel);

                    if (!shouldChangeTask) break;

                    setTask(taskSuggestion);

                    break;

                case TagSuggestion tagSuggestion:

                    TextFieldInfo = TextFieldInfo
                        .RemoveTagQueryFromDescriptionIfNeeded()
                        .AddTag(tagSuggestion);
                    break;
            }
        }

        private async Task createProject(string projectName)
        {
            var projectId = await navigationService.Navigate<EditProjectViewModel, string, long?>(projectName);
            if (projectId == null) return;

            var project = await dataSource.Projects.GetById(projectId.Value);
            var projectSuggestion = new ProjectSuggestion(project);

            await SelectSuggestionCommand.ExecuteAsync(projectSuggestion);
        }

        private void setProject(ProjectSuggestion projectSuggestion)
        {
            clearTagsIfNeeded(workspaceId, projectSuggestion.WorkspaceId);

            workspaceId = projectSuggestion.WorkspaceId;

            TextFieldInfo = TextFieldInfo
                .RemoveProjectQueryFromDescriptionIfNeeded()
                .WithProjectInfo(
                    projectSuggestion.ProjectId,
                    projectSuggestion.ProjectName,
                    projectSuggestion.ProjectColor);
        }

        private void setTask(TaskSuggestion taskSuggestion)
        {
            clearTagsIfNeeded(workspaceId, taskSuggestion.WorkspaceId);

            workspaceId = taskSuggestion.WorkspaceId;

            TextFieldInfo = TextFieldInfo
                .RemoveProjectQueryFromDescriptionIfNeeded()
                .WithProjectAndTaskInfo(
                    taskSuggestion.ProjectId,
                    taskSuggestion.ProjectName,
                    taskSuggestion.ProjectColor,
                    taskSuggestion.TaskId,
                    taskSuggestion.Name
                );
        }

        private void clearTagsIfNeeded(long currenctWorkspaceId, long newWorkspaceId)
        {
            if (currenctWorkspaceId == newWorkspaceId) return;

            TextFieldInfo = TextFieldInfo.ClearTags();
        }

        public override void Prepare(DateTimeOffset parameter)
        {
            StartTime = parameter;

            elapsedTimeDisposable =
                timeService.CurrentDateTimeObservable.Subscribe(currentTime => ElapsedTime = currentTime - StartTime);

            var queryByTypeObservable =
                queryByTypeSubject
                    .AsObservable()
                    .SelectMany(type => dataSource.AutocompleteProvider.Query(new QueryInfo("", type)));

            queryDisposable =
                infoSubject.AsObservable()
                    .StartWith(TextFieldInfo)
                    .Select(dataSource.AutocompleteProvider.ParseFieldInfo)
                    .Do(queryInfo => CurrentQuery = queryInfo.Text)
                    .SelectMany(dataSource.AutocompleteProvider.Query)
                    .Merge(queryByTypeObservable)
                    .Subscribe(onSuggestions);
        }

        public async override Task Initialize()
        {
            await base.Initialize();

            await setBillableValues(0);

            workspaceId = (await dataSource.User.Current()).DefaultWorkspaceId;
        }

        private async void OnTextFieldInfoChanged()
        {
            infoSubject.OnNext(TextFieldInfo);

            if (TextFieldInfo.ProjectId == lastProjectId) return;
            lastProjectId = TextFieldInfo.ProjectId;
            await setBillableValues(lastProjectId ?? 0);
        }

        private void toggleTagSuggestions()
        {
            if (IsSuggestingTags)
            {
                TextFieldInfo = TextFieldInfo.RemoveTagQueryFromDescriptionIfNeeded();
                return;
            }

            var newText = TextFieldInfo.Text.Insert(TextFieldInfo.CursorPosition, QuerySymbols.TagsString);
            TextFieldInfo = TextFieldInfo.WithTextAndCursor(newText, TextFieldInfo.CursorPosition + 1);
        }

        private void toggleProjectSuggestions()
        {
            if (IsSuggestingProjects)
            {
                TextFieldInfo = TextFieldInfo.RemoveProjectQueryFromDescriptionIfNeeded();
                return;
            }

            if (TextFieldInfo.ProjectId != null)
            {
                queryByTypeSubject.OnNext(AutocompleteSuggestionType.Projects);
                IsSuggestingProjects = true;
                return;
            }

            var newText = TextFieldInfo.Text.Insert(TextFieldInfo.CursorPosition, QuerySymbols.ProjectsString);
            TextFieldInfo = TextFieldInfo.WithTextAndCursor(newText, TextFieldInfo.CursorPosition + 1);
        }

        private void toggleTaskSuggestions(ProjectSuggestion projectSuggestion)
        {
            var grouping = Suggestions.FirstOrDefault(s => s.WorkspaceName == projectSuggestion.WorkspaceName);
            if (grouping == null) return;

            var suggestionIndex = grouping.IndexOf(projectSuggestion);
            if (suggestionIndex < 0) return;

            projectSuggestion.TasksVisible = !projectSuggestion.TasksVisible;

            var groupingIndex = Suggestions.IndexOf(grouping);
            Suggestions.Remove(grouping);
            Suggestions.Insert(groupingIndex,
                new WorkspaceGroupedCollection<AutocompleteSuggestion>(
                    grouping.WorkspaceName, getSuggestionsWithTasks(grouping)
                )
            );
        }

        private void toggleBillable() => IsBillable = !IsBillable;

        private async Task changeStartTime()
        {
            IsEditingStartDate = true;

            var currentTime = timeService.CurrentDateTime;
            var minDate = currentTime.AddHours(-MaxTimeEntryDurationInHours);

            var parameters = DatePickerParameters.WithDates(StartTime, minDate, currentTime);
            StartTime = await navigationService
                .Navigate<SelectDateTimeViewModel, DatePickerParameters, DateTimeOffset>(parameters)
                .ConfigureAwait(false);

            IsEditingStartDate = false;
        }

        private Task back() => navigationService.Close(this);

        private async Task changeDuration()
        {
            IsEditingDuration = true;

            var currentDuration = DurationParameter.WithStartAndStop(StartTime, null);
            var selectedDuration = await navigationService
                .Navigate<EditDurationViewModel, DurationParameter, DurationParameter>(currentDuration)
                .ConfigureAwait(false);

            StartTime = selectedDuration.Start;

            IsEditingDuration = false;
        }

        private async Task done()
        {
            await dataSource.User.Current()
                .SelectMany(user =>
                {
                    if (TextFieldInfo.ProjectId == 0)
                        return Observable.Return((User: user, WorkspaceId: workspaceId));

                    if (TextFieldInfo.ProjectId == null)
                        return Observable.Return((User: user, WorkspaceId: user.DefaultWorkspaceId));

                    return dataSource.Projects
                        .GetById(TextFieldInfo.ProjectId.Value)
                        .Select(project => (User: user, WorkspaceId: project.WorkspaceId));
                })
                .Select(tuple => new StartTimeEntryDTO
                {
                    TaskId = TextFieldInfo.TaskId,
                    StartTime = StartTime,
                    Billable = IsBillable,
                    UserId = tuple.User.Id,
                    WorkspaceId = tuple.WorkspaceId,
                    Description = TextFieldInfo.Text,
                    ProjectId = TextFieldInfo.ProjectId,
                    TagIds = TextFieldInfo.Tags.Select(t => t.TagId).Distinct().ToArray()
                })
                .SelectMany(dataSource.TimeEntries.Start)
                .Do(_ => dataSource.SyncManager.PushSync());

            await navigationService.Close(this);
        }

        private void onSuggestions(IEnumerable<AutocompleteSuggestion> suggestions)
        {
            var firstQuerySymbolIndex = TextFieldInfo.Text.IndexOfAny(new char[] { QuerySymbols.Tags, QuerySymbols.Projects });
            if (firstQuerySymbolIndex >= 0)
            {
                var firstQuerySymbol = TextFieldInfo.Text[firstQuerySymbolIndex];
                IsSuggestingTags = firstQuerySymbol == QuerySymbols.Tags;
                IsSuggestingProjects = firstQuerySymbol == QuerySymbols.Projects;
            }
            else
            {
                IsSuggestingTags = IsSuggestingProjects = false;
            }

            Suggestions.Clear();

            var groupedSuggestions = groupSuggestions(suggestions).ToList();

            UseGrouping = groupedSuggestions.Count > 1;
            Suggestions.AddRange(groupedSuggestions);

        }

        private IEnumerable<WorkspaceGroupedCollection<AutocompleteSuggestion>> groupSuggestions(
            IEnumerable<AutocompleteSuggestion> suggestions)
        {
            var firstSuggestion = suggestions.FirstOrDefault();
            if (firstSuggestion is ProjectSuggestion)
                return suggestions.GroupByWorkspaceAddingNoProject();

            if (IsSuggestingTags)
                suggestions = suggestions.Where(suggestion => suggestion.WorkspaceId == workspaceId);

            return suggestions
                .GroupBy(suggestion => suggestion.WorkspaceName)
                .Select(grouping => new WorkspaceGroupedCollection<AutocompleteSuggestion>(
                    grouping.Key, grouping.Distinct(AutocompleteSuggestionComparer.Instance)));
        }

        private async Task setBillableValues(long projectId)
        {
            var workspaceObservable = projectId == 0
                ? dataSource.Workspaces.GetDefault().Select(ws => (Workspace: ws, DefaultToBillable: false))
                : dataSource.Projects.GetById(projectId)
                    .SelectMany(project =>
                        dataSource.Workspaces
                            .GetById(project.WorkspaceId)
                            .Select(ws => (Workspace: ws, DefaultToBillable: project.Billable ?? false)));

            (IsBillableAvailable, IsBillable) =
                await workspaceObservable
                    .SelectMany(tuple =>
                        dataSource.Workspaces
                            .WorkspaceHasFeature(tuple.Workspace.Id, WorkspaceFeatureId.Pro)
                            .Select(isAvailable => (IsBillableAvailable: isAvailable, IsBillable: isAvailable && tuple.DefaultToBillable)));
        }

        private IEnumerable<AutocompleteSuggestion> getSuggestionsWithTasks(
            IEnumerable<AutocompleteSuggestion> suggestions)
        {
            foreach (var suggestion in suggestions)
            {
                if (suggestion is TaskSuggestion) continue;

                yield return suggestion;

                if (suggestion is ProjectSuggestion projectSuggestion && projectSuggestion.TasksVisible)
                    foreach (var taskSuggestion in projectSuggestion.Tasks)
                        yield return taskSuggestion;
            }
        }
    }
}
