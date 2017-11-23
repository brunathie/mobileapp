using MvvmCross.Binding.BindingContext;
using MvvmCross.Binding.iOS;
using MvvmCross.iOS.Views;
using MvvmCross.Plugins.Color;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.ViewModels;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public sealed partial class EditProjectViewController : MvxViewController<EditProjectViewModel>
    {
        public EditProjectViewController() 
            : base(nameof(EditProjectViewController), null)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            prepareViews();

            var bindingSet = this.CreateBindingSet<EditProjectViewController, EditProjectViewModel>();

            //Commands
            bindingSet.Bind(DoneButton).To(vm => vm.DoneCommand);
            bindingSet.Bind(CloseButton).To(vm => vm.CloseCommand);
            bindingSet.Bind(ColorCircleView)
                      .For(v => v.BindTap())
                      .To(vm => vm.PickColorCommand);

            bindingSet.Bind(ClientLabel)
                      .For(v => v.BindTap())
                      .To(vm => vm.PickClientCommand);
            
            bindingSet.Bind(WorkspaceLabel)
                      .For(v => v.BindTap())
                      .To(vm => vm.PickWorkspaceCommand);

            //State
            bindingSet.Bind(NameTextField).To(vm => vm.Name);
            bindingSet.Bind(WorkspaceLabel).To(vm => vm.WorkspaceName);
            bindingSet.Bind(PrivateProjectSwitch).To(vm => vm.IsPrivate);
            bindingSet.Bind(ColorCircleView)
                      .For(v => v.BackgroundColor)
                      .To(vm => vm.Color)
                      .WithConversion(new MvxNativeColorValueConverter());

            bindingSet.Bind(DoneButton)
                      .For(v => v.Enabled)
                      .To(vm => vm.SaveEnabled);

            bindingSet.Bind(ClientLabel)
                      .For(v => v.AttributedText)
                      .To(vm => vm.ClientName)
                      .WithConversion(new AddNewAttributedStringConverter(Resources.AddClient, ClientLabel.Font.CapHeight));

            bindingSet.Apply();
        }

        private void prepareViews()
        {
            PrivateProjectSwitch.Resize();
        }
    }
}

