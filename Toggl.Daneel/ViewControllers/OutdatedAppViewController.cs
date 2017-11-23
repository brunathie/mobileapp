using CoreGraphics;
using MvvmCross.Binding.BindingContext;
using MvvmCross.iOS.Views;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [ModalDialogPresentation]
    public sealed partial class OutdatedAppViewController : MvxViewController<OutdatedAppViewModel>
    {
        private const int cardHeight = 357;

        public OutdatedAppViewController()
            : base(nameof(OutdatedAppViewController), null)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var screenWidth = UIScreen.MainScreen.Bounds.Width;
            PreferredContentSize = new CGSize
            {
                // ScreenWidth - 32 for 16pt margins on both sides
                Width = screenWidth > 320 ? screenWidth - 32 : 312,
                Height = cardHeight
            };

            var bindingSet = this.CreateBindingSet<OutdatedAppViewController, OutdatedAppViewModel>();

            bindingSet.Bind(UpdateButton).To(vm => vm.UpdateAppCommand);
            bindingSet.Bind(WebsiteButton).To(vm => vm.OpenWebsiteCommand);

            bindingSet.Apply();
        }
    }
}
