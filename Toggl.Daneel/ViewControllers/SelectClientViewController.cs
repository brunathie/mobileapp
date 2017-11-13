using MvvmCross.Binding.BindingContext;
using MvvmCross.iOS.Views;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation.MvvmCross.ViewModels;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public partial class SelectClientViewController : MvxViewController<SelectClientViewModel>
    {
        public SelectClientViewController()
            : base(nameof(SelectClientViewController), null)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var source = new ClientTableViewSource(SuggestionsTableView);
            SuggestionsTableView.Source = source;

            var bindingSet = this.CreateBindingSet<SelectClientViewController, SelectClientViewModel>();

            bindingSet.Bind(source).To(vm => vm.Suggestions);
            bindingSet.Bind(SearchTextField).To(vm => vm.Text);
            bindingSet.Bind(CloseButton).To(vm => vm.CloseCommand);
            bindingSet.Bind(source)
                      .For(v => v.SelectionChangedCommand)
                      .To(vm => vm.SelectClientCommand);

            bindingSet.Bind(source)
                      .For(v => v.CreateClientCommand)
                      .To(vm => vm.CreateClientCommand);

            bindingSet.Bind(source)
                      .For(v => v.SuggestCreation)
                      .To(vm => vm.SuggestCreation);

            bindingSet.Bind(source)
                      .For(v => v.Text)
                      .To(vm => vm.Text);

            bindingSet.Apply();

            SearchTextField.BecomeFirstResponder();
        }
    }
}
