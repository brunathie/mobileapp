using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MvvmCross.Core.Navigation;
using MvvmCross.Core.ViewModels;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Sync;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class SettingsViewModel : MvxViewModel
    {
        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        private readonly ITogglDataSource dataSource;
        private readonly IDialogService dialogService;
        private readonly IMvxNavigationService navigationService;

        private long workspaceId;

        public string Title { get; private set; } = Resources.Settings;

        public string Email { get; private set; } = "";

        public string Version { get; private set; } = "";

        public string WorkspaceName { get; private set; } = "";

        public string CurrentPlan { get; private set; } = "";

        public bool UseTwentyFourHourClock { get; set; }

        public bool AddMobileTag { get; set; }

        public bool IsLoggingOut { get; private set; }

        public bool IsRunningSync { get; private set; }

        public bool IsSynced { get; private set; }

        public IMvxCommand RateCommand { get; }
        
        public IMvxCommand HelpCommand { get; }
        
        public IMvxCommand UpdateCommand { get; }

        public IMvxAsyncCommand BackCommand { get; }

        public IMvxAsyncCommand LogoutCommand { get; }

        public IMvxCommand EditProfileCommand { get; }

        public IMvxCommand SubmitFeedbackCommand { get; }

        public IMvxCommand EditSubscriptionCommand { get; }
        
        public IMvxAsyncCommand EditWorkspaceCommand { get; }

        public IMvxCommand ToggleAddMobileTagCommand { get; }

        public IMvxCommand ToggleUseTwentyFourHourClockCommand { get; }

        public SettingsViewModel(ITogglDataSource dataSource, IMvxNavigationService navigationService, IDialogService dialogService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(dialogService, nameof(dialogService));

            this.dataSource = dataSource;
            this.dialogService = dialogService;
            this.navigationService = navigationService;

            disposeBag.Add(dataSource.SyncManager
                .StateObservable
                .Subscribe(async state =>
                {
                    IsRunningSync = IsLoggingOut == false && state != SyncState.Sleep;
                    IsSynced = IsLoggingOut == false && await isSynced();
                })
            );

            RateCommand = new MvxCommand(rate);
            HelpCommand = new MvxCommand(help);
            UpdateCommand = new MvxCommand(update);
            BackCommand = new MvxAsyncCommand(back);
            LogoutCommand = new MvxAsyncCommand(maybeLogout);
            EditProfileCommand = new MvxCommand(editProfile);
            EditWorkspaceCommand = new MvxAsyncCommand(editWorkspace);
            SubmitFeedbackCommand = new MvxCommand(submitFeedback);
            EditSubscriptionCommand = new MvxCommand(editSubscription);
            ToggleAddMobileTagCommand = new MvxCommand(toggleAddMobileTag);
            ToggleUseTwentyFourHourClockCommand = new MvxCommand(toggleUseTwentyFourHourClock);
        }

        public override async Task Initialize()
        {
            var user = await dataSource.User.Current();
            var workspace = await dataSource.Workspaces.GetDefault();

            Email = user.Email; 
            workspaceId = workspace.Id;
            WorkspaceName = workspace.Name;
        }

        public void rate() => throw new NotImplementedException();
        
        public void help() => throw new NotImplementedException();
        
        public void update() => throw new NotImplementedException();
        
        public void editProfile() => throw new NotImplementedException();
        
        public async Task editWorkspace() 
        {
            var parameters = WorkspaceParameters.Create(workspaceId, Resources.SetDefaultWorkspaces, allowQuerying: false);
            var selectedWorkspaceId = 
                await navigationService
                    .Navigate<SelectWorkspaceViewModel, WorkspaceParameters, long>(parameters);

            if (selectedWorkspaceId == workspaceId) return;

            var workspace = await dataSource.Workspaces.GetById(selectedWorkspaceId);
            workspaceId = selectedWorkspaceId;
            WorkspaceName = workspace.Name;

            await dataSource.User.UpdateWorkspace(workspaceId);
            await dataSource.SyncManager.PushSync();
        }
        
        public void submitFeedback() => throw new NotImplementedException();
        
        public void editSubscription() => throw new NotImplementedException();

        public void toggleAddMobileTag() => AddMobileTag = !AddMobileTag;

        public void toggleUseTwentyFourHourClock() => UseTwentyFourHourClock = !UseTwentyFourHourClock;

        private Task back() => navigationService.Close(this);

        private async Task maybeLogout()
        {
            if (await isSynced())
            {
                await logout();
                return;
            }

            var (title, message) = IsRunningSync
                ? (Resources.SettingsSyncInProgressTitle, Resources.SettingsSyncInProgressMessage)
                : (Resources.SettingsUnsyncedTitle, Resources.SettingsUnsyncedMessage);

            var shouldLogout = await dialogService.Confirm(
                title,
                message,
                Resources.SettingsDialogButtonSignOut,
                Resources.Cancel
            );

            if (!shouldLogout) return;

            await logout();
        }

        private async Task logout()
        {
            IsLoggingOut = true;
            IsSynced = false;
            IsRunningSync = false;
            await dataSource.SyncManager.Freeze();
            await dataSource.Logout();
            await navigationService.Navigate<OnboardingViewModel>();
        }

        private async Task<bool> isSynced()
            => !IsRunningSync && !(await dataSource.HasUnsyncedData());
    }
}
