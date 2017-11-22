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
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using static Toggl.Foundation.Helper.Constants;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class SelectTagsViewModel : MvxViewModel<(long[] tagIds, long workspaceId), long[]>
    {
        private readonly IMvxNavigationService navigationService;
        private readonly ITogglDataSource dataSource;
        private readonly Subject<string> textSubject = new Subject<string>();
        private readonly HashSet<long> selectedTagIds = new HashSet<long>();

        private long[] defaultResult;
        private long workspaceId;

        public string Text { get; set; } = "";

        public bool SuggestCreation
        {
            get
            {
                var text = Text.Trim();
                return !string.IsNullOrEmpty(text)
                       && !Tags.Any(tag => tag.Name == text.Trim())
                       && Encoding.UTF8.GetByteCount(Text) <= MaxTagNameLengthInBytes;
            }
        }

        public MvxObservableCollection<SelectableTagViewModel> Tags { get; }
            = new MvxObservableCollection<SelectableTagViewModel>();

        public IMvxAsyncCommand CloseCommand { get; }

        public IMvxAsyncCommand SaveCommand { get; }

        public IMvxAsyncCommand<string> CreateTagCommand { get; }

        public IMvxCommand<SelectableTagViewModel> SelectTagCommand { get; }

        public SelectTagsViewModel(ITogglDataSource dataSource, IMvxNavigationService navigationService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));

            this.dataSource = dataSource;
            this.navigationService = navigationService;

            CloseCommand = new MvxAsyncCommand(close);
            SaveCommand = new MvxAsyncCommand(save);
            CreateTagCommand = new MvxAsyncCommand<string>(createTag);
            SelectTagCommand = new MvxCommand<SelectableTagViewModel>(selectTag);
        }

        public override void Prepare((long[] tagIds, long workspaceId) parameter)
        {
            workspaceId = parameter.workspaceId;
            defaultResult = parameter.tagIds;
            selectedTagIds.AddRange(parameter.tagIds);
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            textSubject.AsObservable()
                       .StartWith(Text)
                       .SelectMany(text => dataSource.AutocompleteProvider.Query(new QueryInfo(text, AutocompleteSuggestionType.Tags)))
                       .Select(suggestions => suggestions.Cast<TagSuggestion>())
                       .Select(suggestions => suggestions.Where(s => s.WorkspaceId == workspaceId))
                       .Subscribe(onTags);
        }

        private void OnTextChanged()
        {
            textSubject.OnNext(Text.Trim());
        }

        private void onTags(IEnumerable<TagSuggestion> tags)
        {
            Tags.Clear();

            var sortedTags = tags.Select(createSelectableTag)
                                 .OrderByDescending(tag => tag.Selected)
                                 .ThenBy(tag => tag.Name);

            Tags.AddRange(sortedTags);
        }

        private SelectableTagViewModel createSelectableTag(TagSuggestion tagSuggestion)
            => new SelectableTagViewModel(tagSuggestion, selectedTagIds.Contains(tagSuggestion.TagId));

        private Task close()
            => navigationService.Close(this, defaultResult);

        private Task save() => navigationService.Close(this, selectedTagIds.ToArray());

        private void selectTag(SelectableTagViewModel tag)
        {
            tag.Selected = !tag.Selected;

            if (tag.Selected)
                selectedTagIds.Add(tag.Id);
            else
                selectedTagIds.Remove(tag.Id);
        }

        private async Task createTag(string name)
        {
            var createdTag = await dataSource.Tags.Create(name.Trim(), workspaceId);
            selectedTagIds.Add(createdTag.Id);
            Text = "";
        }
    }
}
