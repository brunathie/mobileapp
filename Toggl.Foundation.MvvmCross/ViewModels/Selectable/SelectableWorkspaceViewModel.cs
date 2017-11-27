using MvvmCross.Core.ViewModels;
using MvvmCross.Platform.UI;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    public sealed class SelectableWorkspaceViewModel : MvxNotifyPropertyChanged
    {
        public long WorkspaceId { get; set; }

        public string WorkspaceName { get; set; }

        public bool Selected { get; set; }

        public SelectableWorkspaceViewModel(IDatabaseWorkspace workspace, bool selected)
        {
            Ensure.Argument.IsNotNull(workspace, nameof(workspace));

            Selected = selected;
            WorkspaceId = workspace.Id;
            WorkspaceName = workspace.Name;
        }
    }
}
