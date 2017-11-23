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
    public sealed class SelectProjectViewModel
        : MvxViewModel<SelectProjectParameter, SelectProjectParameter>
    {
        private readonly ITogglDataSource dataSource;
        private readonly IMvxNavigationService navigationService;
        private readonly IDialogService dialogService;
        private readonly Subject<string> infoSubject = new Subject<string>();

        private long? taskId;
        private long? projectId;
        private long workspaceId;

        public string Text { get; set; } = "";

        public bool SuggestCreation
        {
            get
            {
                var text = Text.Trim();
                return !string.IsNullOrEmpty(text)
                    && !Suggestions.Any(c => c.Any(s => s is ProjectSuggestion pS && pS.ProjectName == text))
                    && text.LengthInBytes() <= MaxProjectNameLengthInBytes;
            }
        }

        public IMvxAsyncCommand CloseCommand { get; }

        public IMvxAsyncCommand CreateProjectCommand { get; }

        public IMvxCommand<ProjectSuggestion> ToggleTaskSuggestionsCommand { get; }

        public IMvxAsyncCommand<AutocompleteSuggestion> SelectProjectCommand { get; }

        public MvxObservableCollection<WorkspaceGroupedCollection<AutocompleteSuggestion>> Suggestions { get; }
            = new MvxObservableCollection<WorkspaceGroupedCollection<AutocompleteSuggestion>>();

        public SelectProjectViewModel(
            ITogglDataSource dataSource, IMvxNavigationService navigationService, IDialogService dialogService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(dialogService, nameof(dialogService));

            this.dataSource = dataSource;
            this.navigationService = navigationService;
            this.dialogService = dialogService;

            CloseCommand = new MvxAsyncCommand(close);
            CreateProjectCommand = new MvxAsyncCommand(createProject);
            SelectProjectCommand = new MvxAsyncCommand<AutocompleteSuggestion>(selectProject);
            ToggleTaskSuggestionsCommand = new MvxCommand<ProjectSuggestion>(toggleTaskSuggestions);
        }

        public override void Prepare(SelectProjectParameter parameter)
        {
            taskId = parameter.TaskId;
            projectId = parameter.ProjectId;
            workspaceId = parameter.WorkspaceId;
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            infoSubject.AsObservable()
                       .StartWith(Text)
                       .SelectMany(text => dataSource.AutocompleteProvider.Query(new QueryInfo(text, AutocompleteSuggestionType.Projects)))
                       .Select(suggestions => suggestions.Cast<ProjectSuggestion>())
                       .Select(setSelectedProject)
                       .Subscribe(onSuggestions);
        }

        private IEnumerable<ProjectSuggestion> setSelectedProject(IEnumerable<ProjectSuggestion> suggestions)
            => suggestions.Select(s => { s.Selected = s.ProjectId == projectId; return s; });

        private void OnTextChanged()
        {
            infoSubject.OnNext(Text);
        }

        private void onSuggestions(IEnumerable<ProjectSuggestion> suggestions)
        {
            Suggestions.Clear();

            suggestions
                .GroupByWorkspaceAddingNoProject()
                .ForEach(Suggestions.Add);
        }

        private async Task createProject()
        {
            if (!SuggestCreation) return;
            
            var createdProjectId = await navigationService.Navigate<EditProjectViewModel, string, long?>(Text.Trim());
            if (createdProjectId == null) return;
            
            var project = await dataSource.Projects.GetById(createdProjectId.Value);
            var parameter = SelectProjectParameter.WithIds(project.Id, null, project.WorkspaceId);
            await navigationService.Close(this, parameter);
        }

        private Task close()
            => navigationService.Close(
                this,
                SelectProjectParameter.WithIds(projectId, taskId, workspaceId));

        private async Task selectProject(AutocompleteSuggestion suggestion)
        {
            if (suggestion.WorkspaceId == workspaceId || suggestion.WorkspaceId == 0)
            {
                setProject(suggestion);
                return;
            }

            var shouldSetProject = await dialogService.Confirm(
                Resources.DifferentWorkspaceAlertTitle,
                Resources.DifferentWorkspaceAlertMessage,
                Resources.Ok,
                Resources.Cancel
            );

            if (!shouldSetProject) return;

            setProject(suggestion);
        }

        private void setProject(AutocompleteSuggestion suggestion)
        {
            workspaceId = suggestion.WorkspaceId;
            switch (suggestion)
            {
                case ProjectSuggestion projectSuggestion:
                    projectId = projectSuggestion
                        .ProjectId == 0 ? null : (long?)projectSuggestion.ProjectId;
                    taskId = null;
                    break;

                case TaskSuggestion taskSuggestion:
                    projectId = taskSuggestion.ProjectId;
                    taskId = taskSuggestion.TaskId;
                    break;

                default:
                    throw new ArgumentException($"{nameof(suggestion)} must be either of type {nameof(ProjectSuggestion)} or {nameof(TaskSuggestion)}.");
            }

            navigationService.Close(
                this,
                SelectProjectParameter.WithIds(projectId, taskId, workspaceId));
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
