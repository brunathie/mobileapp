using System;
using Foundation;
using MvvmCross.Binding.ExtensionMethods;
using MvvmCross.Binding.iOS.Views;
using MvvmCross.Core.ViewModels;
using Toggl.Daneel.Views;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public sealed class ClientTableViewSource : MvxTableViewSource
    {
        private const string cellIdentifier = nameof(ClientViewCell);
        private const string createEntityCellIdentifier = nameof(CreateEntityViewCell);

        public string Text { get; set; }

        public bool SuggestCreation { get; set; }

        public IMvxCommand CreateClientCommand { get; set; }

        public ClientTableViewSource(UITableView tableView)
            : base(tableView)
        {
            tableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
            tableView.RegisterNibForCellReuse(ClientViewCell.Nib, cellIdentifier);
            tableView.RegisterNibForCellReuse(CreateEntityViewCell.Nib, createEntityCellIdentifier);
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var item = GetItemAt(indexPath);
            var cell = GetOrCreateCellFor(tableView, indexPath, item);
            cell.SelectionStyle = UITableViewCellSelectionStyle.None;

            if (item != null && cell is IMvxBindable bindable)
                bindable.DataContext = item;

            return cell;
        }

        public override nint RowsInSection(UITableView tableview, nint section)
           => base.RowsInSection(tableview, section) + (SuggestCreation ? 1 : 0);

        protected override object GetItemAt(NSIndexPath indexPath)
        {
            if (!SuggestCreation) return base.GetItemAt(indexPath);

            var index = (int)indexPath.Item;
            if (index == 0)
                return $"Create client \"{Text.Trim()}\"";

            return ItemsSource.ElementAt(index - 1);
        }

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            if (SuggestCreation && indexPath.Item == 0)
            {
                CreateClientCommand.Execute();
                return;
            }

            base.RowSelected(tableView, indexPath);
        }

        protected override UITableViewCell GetOrCreateCellFor(UITableView tableView, NSIndexPath indexPath, object item)
        {
            var isCreationCell = SuggestCreation && indexPath.Item == 0;
            var identifier = isCreationCell ? createEntityCellIdentifier : cellIdentifier;
            return tableView.DequeueReusableCell(identifier, indexPath);
        }

        public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath) => 48;
    }
}