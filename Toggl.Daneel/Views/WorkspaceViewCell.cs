using System;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Binding.iOS;
using MvvmCross.Binding.iOS.Views;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.PrimeRadiant.Models;
using UIKit;

namespace Toggl.Daneel.Views
{
    public partial class WorkspaceViewCell : MvxTableViewCell
    {
        public static readonly NSString Key = new NSString(nameof(WorkspaceViewCell));
        public static readonly UINib Nib;

        static WorkspaceViewCell()
        {
            Nib = UINib.FromName(nameof(WorkspaceViewCell), NSBundle.MainBundle);
        }

        protected WorkspaceViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            this.DelayBind(() =>
            {
                var bindingSet = this.CreateBindingSet<WorkspaceViewCell, SelectableWorkspaceViewModel>();

                bindingSet.Bind(NameLabel).To(vm => vm.WorkspaceName);

                bindingSet.Bind(SelectedImage)
                          .For(v => v.BindVisible())
                          .To(vm => vm.Selected);

                bindingSet.Apply();
            });
        }
    }
}
