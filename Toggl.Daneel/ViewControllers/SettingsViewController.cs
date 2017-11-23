using MvvmCross.Binding.BindingContext;
using MvvmCross.Binding.iOS;
using MvvmCross.iOS.Views;
using MvvmCross.iOS.Views.Presenters.Attributes;
using MvvmCross.Plugins.Color.iOS;
using MvvmCross.Plugins.Visibility;
using Toggl.Daneel.Extensions;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [MvxChildPresentation]
    public partial class SettingsViewController : MvxViewController<SettingsViewModel>
    {
        public SettingsViewController() 
            : base(nameof(SettingsViewController), null)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            prepareViews();

            Title = ViewModel.Title;

            var inverseBoolConverter = new BoolToConstantValueConverter<bool>(false, true);
            var visibilityConverter = new MvxVisibilityValueConverter();

            var bindingSet = this.CreateBindingSet<SettingsViewController, SettingsViewModel>();

            // Text
            bindingSet.Bind(EmailLabel).To(vm => vm.Email);
            bindingSet.Bind(VersionLabel).To(vm => vm.Version);
            bindingSet.Bind(PlanLabel).To(vm => vm.CurrentPlan);
            bindingSet.Bind(WorkspaceLabel).To(vm => vm.WorkspaceName);

            // Commands
            bindingSet.Bind(LogoutButton).To(vm => vm.LogoutCommand);
            bindingSet.Bind(EmailView)
                      .For(v => v.BindTap())
                      .To(vm => vm.EditProfileCommand);

            bindingSet.Bind(WorkspaceView)
                      .For(v => v.BindTap())
                      .To(vm => vm.EditWorkspaceCommand);

            bindingSet.Bind(SubscriptionView)
                      .For(v => v.BindTap())
                      .To(vm => vm.EditSubscriptionCommand);

            bindingSet.Bind(TwentyFourHourClockView)
                      .For(v => v.BindTap())
                      .To(vm => vm.ToggleUseTwentyFourHourClockCommand);

            bindingSet.Bind(AddMobileTagView)
                      .For(v => v.BindTap())
                      .To(vm => vm.ToggleAddMobileTagCommand);

            bindingSet.Bind(FeedbackView)
                      .For(v => v.BindTap())
                      .To(vm => vm.SubmitFeedbackCommand);

            bindingSet.Bind(RateView)
                      .For(v => v.BindTap())
                      .To(vm => vm.RateCommand);

            bindingSet.Bind(UpdateView)
                      .For(v => v.BindTap())
                      .To(vm => vm.UpdateCommand);

            bindingSet.Bind(HelpView)
                      .For(v => v.BindTap())
                      .To(vm => vm.HelpCommand);

            // Logout process
            bindingSet.Bind(LogoutButton)
                      .For(btn => btn.Enabled)
                      .To(vm => vm.IsLoggingOut)
                      .WithConversion(inverseBoolConverter);

            bindingSet.Bind(NavigationItem)
                      .For(nav => nav.BindHidesBackButton())
                      .To(vm => vm.IsLoggingOut);

            bindingSet.Bind(SyncingView)
                      .For(view => view.BindVisibility())
                      .To(vm => vm.IsRunningSync)
                      .WithConversion(visibilityConverter);

            bindingSet.Bind(SyncedView)
                      .For(view => view.BindVisibility())
                      .To(vm => vm.IsSynced)
                      .WithConversion(visibilityConverter);

            bindingSet.Bind(LoggingOutView)
                      .For(view => view.BindVisibility())
                      .To(vm => vm.IsLoggingOut)
                      .WithConversion(visibilityConverter);

            // Switches
            bindingSet.Bind(AddMobileTagSwitch)
                      .For(v => v.BindAnimatedOn())
                      .To(vm => vm.AddMobileTag);
            
            bindingSet.Bind(TwentyFourHourClockSwitch)
                      .For(v => v.BindAnimatedOn())
                      .To(vm => vm.UseTwentyFourHourClock);

            bindingSet.Apply();
        }

        private void prepareViews()
        {
            // Syncing indicator colors
            setIndicatorSyncColor(SyncedIcon);
            setIndicatorSyncColor(SyncingIndicator);
            setIndicatorSyncColor(LoggingOutIndicator);

            // Resize Switches
            AddMobileTagSwitch.Resize();
            TwentyFourHourClockSwitch.Resize();

            // Disable unused settings
            SubscriptionView.Hidden = true;
            TwentyFourHourClockView.Hidden = true;
            AddMobileTagView.Hidden = true;
            FeedbackView.Hidden = true;
            RateView.Hidden = true;
            UpdateView.Hidden = true;
            HelpView.Hidden = true;
        }

        private void setIndicatorSyncColor(UIImageView imageView)
        {
            imageView.Image = imageView.Image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
            imageView.TintColor = Color.Settings.SyncStatusText.ToNativeColor();
        }
    }
}
