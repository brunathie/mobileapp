using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using MvvmCross.Binding.ExtensionMethods;
using MvvmCross.Binding.iOS.Views;
using MvvmCross.Core.ViewModels;
using Toggl.Daneel.Views;
using Toggl.Daneel.Views.Interfaces;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public abstract class GroupedCollectionTableViewSource<T> : MvxTableViewSource
        where T : class
    {
        private readonly string cellIdentifier;
        private readonly string headerCellIdentifier;
        
        protected IEnumerable<MvxObservableCollection<T>> GroupedItems
            => ItemsSource as IEnumerable<MvxObservableCollection<T>>;

        protected GroupedCollectionTableViewSource(UITableView tableView, string cellIdentifier, string headerCellIdentifier)
            : base(tableView)
        {
            this.cellIdentifier = cellIdentifier;
            this.headerCellIdentifier = headerCellIdentifier;
        }

        public override UIView GetViewForHeader(UITableView tableView, nint section)
        {
            var grouping = GetGroupAt(section);

            var cell = GetOrCreateHeaderViewFor(tableView);
            if (cell is IMvxBindable bindable)
                bindable.DataContext = grouping;

            if (section == 0 && cell is IHeaderViewCellWithHideableTopSeparator headerCell)
                headerCell.TopSeparatorHidden = true;

            return cell;
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var item = GetItemAt(indexPath);

            var cell = GetOrCreateCellFor(tableView, indexPath, item);
            if (cell is IMvxBindable bindable)
                bindable.DataContext = item;

            cell.SelectionStyle = UITableViewCellSelectionStyle.None;

            return cell;
        }

        public override nint NumberOfSections(UITableView tableView)
            => ItemsSource.Count();

        public override nint RowsInSection(UITableView tableview, nint section)
            => GetGroupAt(section).Count();

        protected IEnumerable<T> GetGroupAt(nint section)
            => GroupedItems.ElementAtOrDefault((int)section) ?? new MvxObservableCollection<T>();

        protected override object GetItemAt(NSIndexPath indexPath)
            => GroupedItems.ElementAtOrDefault(indexPath.Section)?.ElementAtOrDefault((int)indexPath.Item);

        public override void HeaderViewDisplayingEnded(UITableView tableView, UIView headerView, nint section)
        {
            var firstVisible = TableView.IndexPathsForVisibleRows.First();
            if (firstVisible.Section != section + 1) return;

            var nextHeader = TableView.GetHeaderView(firstVisible.Section) as IHeaderViewCellWithHideableTopSeparator;
            if (nextHeader == null) return;

            nextHeader.TopSeparatorHidden = true;
        }

        public override void WillDisplayHeaderView(UITableView tableView, UIView headerView, nint section)
        {
            var headerViewCell = headerView as IHeaderViewCellWithHideableTopSeparator;
            if (headerViewCell == null) return;

            var firstVisibleIndexPath = TableView.IndexPathsForVisibleRows.First();
            if (firstVisibleIndexPath.Section == section)
            {
                var nextHeader = TableView.GetHeaderView(section + 1) as IHeaderViewCellWithHideableTopSeparator;
                if (nextHeader == null) return;
                nextHeader.TopSeparatorHidden = false;
                headerViewCell.TopSeparatorHidden = true;
            }
            else
            {
                headerViewCell.TopSeparatorHidden = false;
            }
        }

        protected virtual UITableViewHeaderFooterView GetOrCreateHeaderViewFor(UITableView tableView)
            => tableView.DequeueReusableHeaderFooterView(headerCellIdentifier);

        protected override UITableViewCell GetOrCreateCellFor(UITableView tableView, NSIndexPath indexPath, object item)
            => tableView.DequeueReusableCell(cellIdentifier, indexPath);
    }
}
