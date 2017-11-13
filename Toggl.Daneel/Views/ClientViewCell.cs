using System;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Binding.iOS.Views;
using UIKit;

namespace Toggl.Daneel.Views
{
    public partial class ClientViewCell : MvxTableViewCell
    {
        public static readonly NSString Key = new NSString(nameof(ClientViewCell));
        public static readonly UINib Nib;

        static ClientViewCell()
        {
            Nib = UINib.FromName(nameof(ClientViewCell), NSBundle.MainBundle);
        }

        protected ClientViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            this.DelayBind(() =>
            {
                var bindingSet = this.CreateBindingSet<ClientViewCell, string>();

                bindingSet.Bind(NameLabel).To(vm => vm);

                bindingSet.Apply();
            });
        }
    }
}