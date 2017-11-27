using System;
using System.Collections.Generic;
using CoreText;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Core.ViewModels;
using MvvmCross.Binding.iOS;
using MvvmCross.iOS.Views;
using MvvmCross.Plugins.Color.iOS;
using MvvmCross.Plugins.Visibility;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public partial class StartTimeEntryViewController : MvxViewController<StartTimeEntryViewModel>
    {
        public StartTimeEntryViewController() 
            : base(nameof(StartTimeEntryViewController), null)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            prepareViews();

            UIKeyboard.Notifications.ObserveWillShow(keyboardWillShow);
            UIKeyboard.Notifications.ObserveWillHide(keyboardWillHide);

            var source = new StartTimeEntryTableViewSource(SuggestionsTableView);
            SuggestionsTableView.Source = source;
            source.ToggleTasksCommand = new MvxCommand<ProjectSuggestion>(toggleTaskSuggestions);

            var timeSpanConverter = new TimeSpanToDurationValueConverter();
            var invertedVisibilityConverter = new MvxInvertedVisibilityValueConverter();
            var invertedBoolConverter = new BoolToConstantValueConverter<bool>(false, true);
            var buttonColorConverter = new BoolToConstantValueConverter<UIColor>(
                Color.StartTimeEntry.ActiveButton.ToNativeColor(),
                Color.StartTimeEntry.InactiveButton.ToNativeColor()
            );

            var bindingSet = this.CreateBindingSet<StartTimeEntryViewController, StartTimeEntryViewModel>();

            //TableView
            bindingSet.Bind(source).To(vm => vm.Suggestions);

            bindingSet.Bind(source)
                      .For(v => v.UseGrouping)
                      .To(vm => vm.UseGrouping);

            bindingSet.Bind(source)
                      .For(v => v.SelectionChangedCommand)
                      .To(vm => vm.SelectSuggestionCommand);

            bindingSet.Bind(source)
                      .For(v => v.CreateCommand)
                      .To(vm => vm.CreateCommand);
            
            bindingSet.Bind(source)
                      .For(v => v.IsSuggestingProjects)
                      .To(vm => vm.IsSuggestingProjects);
            
            bindingSet.Bind(source)
                      .For(v => v.Text)
                      .To(vm => vm.CurrentQuery);

            bindingSet.Bind(source)
                      .For(v => v.SuggestCreation)
                      .To(vm => vm.SuggestCreation);
            
            //Text
            bindingSet.Bind(TimeLabel)
                      .To(vm => vm.ElapsedTime)
                      .WithConversion(timeSpanConverter);

            bindingSet.Bind(DescriptionTextView)
                      .For(v => v.BindTextFieldInfo())
                      .To(vm => vm.TextFieldInfo);

            bindingSet.Bind(DescriptionRemainingLengthLabel)
                      .To(vm => vm.DescriptionRemainingBytes);

            //Buttons
            bindingSet.Bind(TagsButton)
                      .For(v => v.TintColor)
                      .To(vm => vm.IsSuggestingTags)
                      .WithConversion(buttonColorConverter);
            
            bindingSet.Bind(BillableButton)
                      .For(v => v.TintColor)
                      .To(vm => vm.IsBillable)
                      .WithConversion(buttonColorConverter);

            bindingSet.Bind(ProjectsButton)
                      .For(v => v.TintColor)
                      .To(vm => vm.IsSuggestingProjects)
                      .WithConversion(buttonColorConverter);

            bindingSet.Bind(DurationButton)
                      .For(v => v.TintColor)
                      .To(vm => vm.IsEditingDuration)
                      .WithConversion(buttonColorConverter);
                      
            bindingSet.Bind(DateTimeButton)
                      .For(v => v.TintColor)
                      .To(vm => vm.IsEditingStartDate)
                      .WithConversion(buttonColorConverter);

            bindingSet.Bind(DoneButton)
                      .For(v => v.Enabled)
                      .To(vm => vm.DescriptionLengthExceeded)
                      .WithConversion(invertedBoolConverter);

            //Visibility
            bindingSet.Bind(BillableButtonWidthConstraint)
                      .For(v => v.Constant)
                      .To(vm => vm.IsBillableAvailable)
                      .WithConversion(new BoolToConstantValueConverter<nfloat>(42, 0));

            bindingSet.Bind(DescriptionRemainingLengthLabel)
                      .For(v => v.BindVisible())
                      .To(vm => vm.DescriptionLengthExceeded)
                      .WithConversion(invertedVisibilityConverter);

            //Commands
            bindingSet.Bind(DoneButton).To(vm => vm.DoneCommand);
            bindingSet.Bind(CloseButton).To(vm => vm.BackCommand);
            bindingSet.Bind(DurationButton).To(vm => vm.ChangeDurationCommand);
            bindingSet.Bind(BillableButton).To(vm => vm.ToggleBillableCommand);
            bindingSet.Bind(DateTimeButton).To(vm => vm.ChangeStartTimeCommand);
            bindingSet.Bind(TagsButton).To(vm => vm.ToggleTagSuggestionsCommand);
            bindingSet.Bind(ProjectsButton).To(vm => vm.ToggleProjectSuggestionsCommand);

            bindingSet.Apply();
        }

        private void keyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            BottomDistanceConstraint.Constant = e.FrameEnd.Height;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        private void keyboardWillHide(object sender, UIKeyboardEventArgs e)
        {
            BottomDistanceConstraint.Constant = 0;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        private void prepareViews()
        {
            //This is needed for the ImageView.TintColor bindings to work
            foreach (var button in getButtons())
            {
                button.SetImage(
                    button.ImageForState(UIControlState.Normal)
                          .ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate),
                    UIControlState.Normal
                );
            }

            TimeLabel.Font = TimeLabel.Font.GetMonospacedDigitFont();

            var stringAttributes = new CTStringAttributes(
                new UIStringAttributes { ForegroundColor = Color.StartTimeEntry.Placeholder.ToNativeColor() }.Dictionary
            );

            DescriptionTextView.TintColor = Color.StartTimeEntry.Cursor.ToNativeColor();
            DescriptionTextView.BecomeFirstResponder();
        }

        private IEnumerable<UIButton> getButtons()
        {
            yield return TagsButton;
            yield return ProjectsButton;
            yield return BillableButton;
            yield return DurationButton;
            yield return DateTimeButton;
        }

        private void toggleTaskSuggestions(ProjectSuggestion parameter)
        {
            var offset = SuggestionsTableView.ContentOffset;
            var frameHeight = SuggestionsTableView.Frame.Height;

            ViewModel.ToggleTaskSuggestionsCommand.Execute(parameter);

            SuggestionsTableView.CorrectOffset(offset, frameHeight);
        }
    }
}

