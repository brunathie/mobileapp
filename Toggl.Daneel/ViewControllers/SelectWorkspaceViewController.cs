using System;
using MvvmCross.Binding.BindingContext;
using MvvmCross.iOS.Views;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.ViewModels;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public partial class SelectWorkspaceViewController : MvxViewController<SelectWorkspaceViewModel>
    {
        public SelectWorkspaceViewController() 
            : base(nameof(SelectWorkspaceViewController), null)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var source = new WorkspaceTableViewSource(SuggestionsTableView);
            SuggestionsTableView.Source = source;

            var bindingSet = this.CreateBindingSet<SelectWorkspaceViewController, SelectWorkspaceViewModel>();

            bindingSet.Bind(TitleLabel).To(vm => vm.Title);

            bindingSet.Bind(source).To(vm => vm.Suggestions);
            bindingSet.Bind(SearchTextField).To(vm => vm.Text);
            bindingSet.Bind(CloseButton).To(vm => vm.CloseCommand);
            bindingSet.Bind(source)
                      .For(v => v.SelectionChangedCommand)
                      .To(vm => vm.SelectWorkspaceCommand);

            bindingSet.Bind(SuggestionsTableViewConstraint)
                      .For(v => v.Constant)
                      .To(vm => vm.AllowQuerying)
                      .WithConversion(new BoolToConstantValueConverter<nfloat>(72, 28));

            bindingSet.Apply();

            SearchTextField.BecomeFirstResponder();
        }
    }
}

