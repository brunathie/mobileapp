using System;
using System.Linq;
using Foundation;
using MvvmCross.Binding.ExtensionMethods;
using MvvmCross.Core.ViewModels;
using MvvmCross.Plugins.Color.iOS;
using Toggl.Daneel.Views;
using Toggl.Foundation;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.MvvmCross.Helper;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public sealed class StartTimeEntryTableViewSource : CreateSuggestionGroupedTableViewSource<AutocompleteSuggestion>
    {
        private const string tagCellIdentifier = nameof(TagSuggestionViewCell);
        private const string taskCellIdentifier = nameof(TaskSuggestionViewCell);
        private const string headerCellIdentifier = nameof(WorkspaceHeaderViewCell);
        private const string timeEntryCellIdentifier = nameof(StartTimeEntryViewCell);
        private const string projectCellIdentifier = nameof(ProjectSuggestionViewCell);
        private const string emptySuggestionIdentifier = nameof(StartTimeEntryEmptyViewCell);

        public bool UseGrouping { get; set; }

        public bool IsSuggestingProjects { get; set; }

        public IMvxCommand<ProjectSuggestion> ToggleTasksCommand { get; set; }

        public StartTimeEntryTableViewSource(UITableView tableView)
            : base(tableView, headerCellIdentifier, "")
        {
            tableView.TableFooterView = new UIView();
            tableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
            tableView.SeparatorColor = Color.StartTimeEntry.SeparatorColor.ToNativeColor();
            tableView.RegisterNibForCellReuse(TagSuggestionViewCell.Nib, tagCellIdentifier);
            tableView.RegisterNibForCellReuse(TaskSuggestionViewCell.Nib, taskCellIdentifier);
            tableView.RegisterNibForCellReuse(StartTimeEntryViewCell.Nib, timeEntryCellIdentifier);
            tableView.RegisterNibForCellReuse(ProjectSuggestionViewCell.Nib, projectCellIdentifier);
            tableView.RegisterNibForCellReuse(StartTimeEntryEmptyViewCell.Nib, emptySuggestionIdentifier);
            tableView.RegisterNibForHeaderFooterViewReuse(WorkspaceHeaderViewCell.Nib, headerCellIdentifier);
        }

        public override UIView GetViewForHeader(UITableView tableView, nint section)
        {
            if (!UseGrouping) return null;

            return base.GetViewForHeader(tableView, section);
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var cell = base.GetCell(tableView, indexPath);
            cell.LayoutMargins = UIEdgeInsets.Zero;
            cell.SeparatorInset = UIEdgeInsets.Zero;
            cell.PreservesSuperviewLayoutMargins = false;

            if (cell is ProjectSuggestionViewCell projectCell)
            {
                projectCell.ToggleTasksCommand = ToggleTasksCommand;

                var previousItemPath = NSIndexPath.FromItemSection(indexPath.Item - 1, indexPath.Section);
                var previous = GetItemAt(previousItemPath);
                var previousIsTask = previous is TaskSuggestion;
                projectCell.TopSeparatorHidden = !previousIsTask;

                var nextItemPath = NSIndexPath.FromItemSection(indexPath.Item + 1, indexPath.Section);
                var next = GetItemAt(nextItemPath);
                var isLastItemInSection = next == null;
                var isLastSection = indexPath.Section == tableView.NumberOfSections() - 1;
                projectCell.BottomSeparatorHidden = isLastItemInSection && !isLastSection;
            }

            return cell;
        }

        public override nint RowsInSection(UITableView tableview, nint section)
        {
            if (UseGrouping) return base.RowsInSection(tableview, section);

            return GetGroupAt(section).Count() + (SuggestCreation ? 1 : 0);
        }

        public override nint NumberOfSections(UITableView tableView)
        {
            if (!UseGrouping) return 1;

            return base.NumberOfSections(tableView);
        }

        protected override object GetItemAt(NSIndexPath indexPath)
        {
            if (!UseGrouping && SuggestCreation)
            {
                var index = (int)indexPath.Item - 1;
                if (index < 0) return GetCreateSuggestionItem();

                var newIndexPath = NSIndexPath.FromRowSection(indexPath.Section, index);
                return GroupedItems.ElementAtOrDefault(indexPath.Section)?.ElementAtOrDefault(index);
            }

            return base.GetItemAt(indexPath);
        }

        protected override UITableViewHeaderFooterView GetOrCreateHeaderViewFor(UITableView tableView)
            => tableView.DequeueReusableHeaderFooterView(headerCellIdentifier);

        protected override UITableViewCell GetOrCreateCellFor(UITableView tableView, NSIndexPath indexPath, object item)
            => tableView.DequeueReusableCell(getIdentifier(item), indexPath);

        public override nfloat GetHeightForHeader(UITableView tableView, nint section)
            => !UseGrouping ? 0 : base.GetHeightForHeader(tableView, section);

        public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath) => 48;

        private string getIdentifier(object item)
        {
            if (item is string)
                return CreateEntityCellIdentifier;

            if (item is ProjectSuggestion)
                return projectCellIdentifier;

            if (item is QuerySymbolSuggestion)
                return emptySuggestionIdentifier;

            if (item is TagSuggestion)
                return tagCellIdentifier;

            if (item is TaskSuggestion)
                return taskCellIdentifier;

            return timeEntryCellIdentifier;
        }

        protected override object GetCreateSuggestionItem()
            => IsSuggestingProjects
                ? $"{Resources.CreateProject} \"{Text}\""
                : $"{Resources.CreateTag} \"{Text}\"";
    }
}
