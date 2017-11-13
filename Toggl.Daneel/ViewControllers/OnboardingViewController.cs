using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Binding.iOS;
using MvvmCross.iOS.Views;
using MvvmCross.iOS.Views.Presenters.Attributes;
using MvvmCross.Plugins.Color;
using MvvmCross.Plugins.Visibility;
using Toggl.Daneel.Extensions;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [MvxRootPresentation(WrapInNavigationController = true)]
    public sealed partial class OnboardingViewController : MvxViewController<OnboardingViewModel>
    {
        private readonly TrackPage trackPagePlaceholder = TrackPage.Create();
        private readonly LogPage logPagePlaceholder = LogPage.Create();
        private readonly SummaryPage summaryPagePlaceholder = SummaryPage.Create();

        public OnboardingViewController() 
            : base(nameof(OnboardingViewController), null)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            preparePlaceholders();

            PageControl.Pages = ViewModel.NumberOfPages;
            FirstPageLabel.Text = Resources.OnboardingTrackPageCopy;
            SecondPageLabel.Text = Resources.OnboardingLogPageCopy;
            ThirdPageLabel.Text = Resources.OnboardingSummaryPageCopy;

            var visibilityConverter = new MvxVisibilityValueConverter();
            var invertedVisibilityConverter = new MvxInvertedVisibilityValueConverter();
            var colorConverter = new MvxNativeColorValueConverter();
            var bindingSet = this.CreateBindingSet<OnboardingViewController, OnboardingViewModel>();

            //Commands
            bindingSet.Bind(Skip).To(vm => vm.SkipCommand);
            bindingSet.Bind(Next).To(vm => vm.NextCommand);
            bindingSet.Bind(Login).To(vm => vm.LoginCommand);
            bindingSet.Bind(SignUp).To(vm => vm.SignUpCommand);
            bindingSet.Bind(Previous).To(vm => vm.PreviousCommand);

            //Color
            bindingSet.Bind(View)
                      .For(v => v.BindAnimatedBackground())
                      .To(vm => vm.BackgroundColor)
                      .WithConversion(colorConverter);
            
            bindingSet.Bind(PhoneFrame)
                      .For(v => v.BindAnimatedBackground())
                      .To(vm => vm.BorderColor)
                      .WithConversion(colorConverter);

            //Visibility
            bindingSet.Bind(PhoneContents)
                      .For(v => v.BindVisibility())
                      .To(vm => vm.IsLastPage)
                      .WithConversion(invertedVisibilityConverter);

            bindingSet.Bind(LastPageItems)
                      .For(v => v.BindVisibility())
                      .To(vm => vm.IsLastPage)
                      .WithConversion(visibilityConverter);

            bindingSet.Bind(Next)
                      .For(v => v.BindVisibility())
                      .To(vm => vm.IsLastPage)
                      .WithConversion(invertedVisibilityConverter);

            bindingSet.Bind(ScrollView)
                      .For(v => v.BindVisibility())
                      .To(vm => vm.IsLastPage)
                      .WithConversion(invertedVisibilityConverter);

            bindingSet.Bind(Skip)
                      .For(v => v.BindVisibility())
                      .To(vm => vm.IsLastPage)
                      .WithConversion(invertedVisibilityConverter);

            bindingSet.Bind(Previous)
                      .For(v => v.BindVisibility())
                      .To(vm => vm.IsFirstPage)
                      .WithConversion(invertedVisibilityConverter);

            bindingSet.Bind(trackPagePlaceholder)
                      .For(v => v.BindVisibility())
                      .To(vm => vm.IsTrackPage)
                      .WithConversion(visibilityConverter);

            bindingSet.Bind(logPagePlaceholder)
                      .For(v => v.BindVisibility())
                      .To(vm => vm.IsLogPage)
                      .WithConversion(visibilityConverter);

            bindingSet.Bind(summaryPagePlaceholder)
                      .For(v => v.BindVisibility())
                      .To(vm => vm.IsSummaryPage)
                      .WithConversion(visibilityConverter);


            //Current Page
            bindingSet.Bind(ScrollView)
                      .For(v => v.BindCurrentPage())
                      .To(vm => vm.CurrentPage);

            bindingSet.Bind(PageControl)
                      .For(v => v.CurrentPage)
                      .To(vm => vm.CurrentPage);

            bindingSet.Apply();
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            NavigationController.NavigationBar.UserInteractionEnabled = false;
        }

        public override bool PrefersStatusBarHidden()
            => true;

        private void preparePlaceholders()
        {
            PhoneContents.AddSubview(trackPagePlaceholder);
            PhoneContents.AddSubview(logPagePlaceholder);
            PhoneContents.AddSubview(summaryPagePlaceholder);

            setPlaceholderConstraints(trackPagePlaceholder);
            setPlaceholderConstraints(logPagePlaceholder);
            setPlaceholderConstraints(summaryPagePlaceholder);
        }

        private void setPlaceholderConstraints(UIView view)
        {
            view.TopAnchor.ConstraintEqualTo(PhoneContents.TopAnchor).Active = true;
            view.BottomAnchor.ConstraintEqualTo(PhoneContents.BottomAnchor).Active = true;
            view.LeadingAnchor.ConstraintEqualTo(PhoneContents.LeadingAnchor).Active = true;
            view.TrailingAnchor.ConstraintEqualTo(PhoneContents.TrailingAnchor).Active = true;
        }
    }
}
