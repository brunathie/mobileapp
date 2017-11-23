using MvvmCross.Binding.BindingContext;
using MvvmCross.iOS.Views;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public sealed partial class SelectTagsViewController : MvxViewController<SelectTagsViewModel>
    {
        public SelectTagsViewController() : base(nameof(SelectTagsViewController), null)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var source = new SelectTagsTableViewSource(TagsTableView);
            TagsTableView.Source = source;
            TagsTableView.TableFooterView = new UIView();

            var bindingSet = this.CreateBindingSet<SelectTagsViewController, SelectTagsViewModel>();

            //Table view
            bindingSet.Bind(source).To(vm => vm.Tags);

            bindingSet.Bind(source)
                      .For(v => v.CurrentQuery)
                      .To(vm => vm.Text);

            bindingSet.Bind(source)
                      .For(v => v.CreateTagCommand)
                      .To(vm => vm.CreateTagCommand);

            bindingSet.Bind(source)
                      .For(v => v.SuggestCreation)
                      .To(vm => vm.SuggestCreation);
                           
            //Text
            bindingSet.Bind(TextField).To(vm => vm.Text);

            //Commands
            bindingSet.Bind(CloseButton).To(vm => vm.CloseCommand);
            bindingSet.Bind(SaveButton).To(vm => vm.SaveCommand);
            bindingSet.Bind(source)
                      .For(v => v.SelectionChangedCommand)
                      .To(vm => vm.SelectTagCommand);

            bindingSet.Apply();
        }
    }
}
