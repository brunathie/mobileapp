using System;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using MvvmCross.Core.Navigation;
using MvvmCross.Core.ViewModels;
using MvvmCross.Platform.UI;
using PropertyChanged;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Models;
using static Toggl.Foundation.Helper.Constants;
using static Toggl.Multivac.Extensions.StringExtensions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class EditProjectViewModel : MvxViewModel<string, long?>
    {
        private readonly Random random = new Random();
        private readonly ITogglDataSource dataSource;
        private readonly IMvxNavigationService navigationService;

        private bool isPro;
        private long? clientId;
        private long workspaceId;

        public bool IsPrivate { get; set; }

        [DependsOn(nameof(Name))]
        public bool SaveEnabled => 
            !string.IsNullOrWhiteSpace(Name) && Name.LengthInBytes() <= MaxProjectNameLengthInBytes;

        public string Name { get; set; } = "";

        public string TrimmedName => Name.Trim();

        public MvxColor Color { get; set; }

        public string Title { get; private set; } = "";

        public string ClientName { get; private set; } = "";

        public string WorkspaceName { get; private set; } = "";
        
        public string DoneButtonText { get; private set; } = "";

        public IMvxAsyncCommand DoneCommand { get; }

        public IMvxAsyncCommand CloseCommand { get; }

        public IMvxAsyncCommand PickClientCommand { get; }

        public IMvxAsyncCommand PickColorCommand { get; }

        public IMvxAsyncCommand PickWorkspaceCommand { get; }

        public EditProjectViewModel(ITogglDataSource dataSource, IMvxNavigationService navigationService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));

            this.dataSource = dataSource;
            this.navigationService = navigationService;

            DoneCommand = new MvxAsyncCommand(done);
            CloseCommand = new MvxAsyncCommand(close);
            PickColorCommand = new MvxAsyncCommand(pickColor);
            PickClientCommand = new MvxAsyncCommand(pickClient);
            PickWorkspaceCommand = new MvxAsyncCommand(pickWorkspace);
        }

        public override void Prepare(string parameter)
        {
            Name = parameter;
            Title = Resources.NewProject;
            DoneButtonText = Resources.Create;
            pickRandomColor();
        }

        public override async Task Initialize()
        {
            var workspace = await dataSource.Workspaces.GetDefault();
            isPro = await dataSource.Workspaces.WorkspaceHasFeature(workspace.Id, WorkspaceFeatureId.Pro);
            workspaceId = workspace.Id;
            WorkspaceName = workspace.Name;
        }

        private async Task pickColor()
        {
            Color = await navigationService.Navigate<SelectColorViewModel, ColorParameters, MvxColor>(
                ColorParameters.Create(Color, isPro));
        }

        private async Task done()
        {
            if (!SaveEnabled) return;
            
            var workspace = 
                await (isPro 
                ? dataSource.Workspaces.GetById(workspaceId) 
                : Observable.Return(default(IDatabaseWorkspace)));
            
            var billable = workspace?.ProjectsBillableByDefault;

            var createdProject = await dataSource.Projects.Create(new CreateProjectDTO
            {
                Name = TrimmedName,
                Color = $"#{Color.R:X2}{Color.G:X2}{Color.B:X2}",
                IsPrivate = IsPrivate,
                ClientId = clientId,
                Billable = billable,
                WorkspaceId = workspaceId
            });

            await navigationService.Close(this, createdProject.Id);
        }

        private Task close()
            => navigationService.Close(this, null);

        private async Task pickWorkspace()
        {
            var parameters = WorkspaceParameters.Create(workspaceId, Resources.Workspaces, allowQuerying: true);
            var selectedWorkspaceId = 
                await navigationService
                    .Navigate<SelectWorkspaceViewModel, WorkspaceParameters, long>(parameters);

            if (selectedWorkspaceId == workspaceId) return;

            var workspace = await dataSource.Workspaces.GetById(selectedWorkspaceId);

            clientId = null;
            ClientName = "";
            workspaceId = selectedWorkspaceId;
            WorkspaceName = workspace.Name;

            isPro = await dataSource.Workspaces.WorkspaceHasFeature(workspaceId, WorkspaceFeatureId.Pro);
            if (isPro || Array.IndexOf(Helper.Color.DefaultProjectColors, Color) >= 0) return;

            pickRandomColor();
        }
        
        private async Task pickClient()
        {
            var selectedClientId = await navigationService.Navigate<SelectClientViewModel, long, long?>(workspaceId);
            if (selectedClientId == null) return;

            if (selectedClientId.Value == 0)
            {
                selectedClientId = null;
                ClientName = "";
                return;
            }

            var client = await dataSource.Clients.GetById(selectedClientId.Value);
            clientId = client.Id;
            ClientName = client.Name;
        }
        
        private void pickRandomColor()
        {
            var randomColorIndex = random.Next(0, Helper.Color.DefaultProjectColors.Length);
            Color = Helper.Color.DefaultProjectColors[randomColorIndex];
        }
    }
}
