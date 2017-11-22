using System;
using System.Linq;
using Foundation;
using MvvmCross.Binding.ExtensionMethods;
using MvvmCross.Binding.iOS.Views;
using MvvmCross.Core.ViewModels;
using Toggl.Daneel.Views;
using Toggl.Foundation;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public sealed class SelectTagsTableViewSource : MvxTableViewSource
    {
        private const string tagCellIdentifier = nameof(SelectableTagViewCell);
        private const string createCellIdentifier = nameof(CreateEntityViewCell);

        public string CurrentQuery { get; set; }

        public bool SuggestCreation { get; set; }

        public IMvxCommand<string> CreateTagCommand { get; set; }

        public SelectTagsTableViewSource(UITableView tableView) 
            : base(tableView)
        {
            tableView.RegisterNibForCellReuse(SelectableTagViewCell.Nib, tagCellIdentifier);
            tableView.RegisterNibForCellReuse(CreateEntityViewCell.Nib, createCellIdentifier);
        }

        public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath) => 48;

        protected override UITableViewCell GetOrCreateCellFor(UITableView tableView, NSIndexPath indexPath, object item)
            => tableView.DequeueReusableCell(item is string ? createCellIdentifier : tagCellIdentifier, indexPath);

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            tableView.DeselectRow(indexPath, true);

            if (SuggestCreation && indexPath.Item == 0)
            {
                CreateTagCommand.Execute(CurrentQuery);
                return;
            }

            base.RowSelected(tableView, indexPath);
        }

        protected override object GetItemAt(NSIndexPath indexPath)
        {
            if (!SuggestCreation) return base.GetItemAt(indexPath);

            if (indexPath.Item == 0)
                return $"{Resources.CreateTag} \"{CurrentQuery.Trim()}\"";

            return ItemsSource.ElementAt((int)indexPath.Item - 1);
        }

        public override nint RowsInSection(UITableView tableview, nint section)
            => base.RowsInSection(tableview, section) + (SuggestCreation ? 1 : 0);

        public override nint NumberOfSections(UITableView tableView)
            => 1;
    }
}
