using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MvvmCross.Core.Navigation;
using MvvmCross.Core.ViewModels;
using MvvmCross.Platform;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Ultrawave.Exceptions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class TokenResetViewModel : MvxViewModel
    {
        private readonly ILoginManager loginManager;
        private readonly ITogglDataSource dataSource;
        private readonly IDialogService dialogService;
        private readonly IMvxNavigationService navigationService;

        private bool needsSync;

        public string Email { get; private set; }

        public string Password { get; set; }

        public string Error { get; set; }

        public bool IsPasswordMasked { get; private set; } = true;

        public bool NextIsEnabled => !string.IsNullOrEmpty(Password) && !IsLoading;

        public bool HasError => !string.IsNullOrEmpty(Error);

        public IMvxCommand DoneCommand { get; }

        public IMvxAsyncCommand SignOutCommand { get; }

        public IMvxCommand TogglePasswordVisibilityCommand { get; }

        public bool IsLoading { get; private set; }

        public TokenResetViewModel(
            ILoginManager loginManager,
            ITogglDataSource dataSource,
            IDialogService dialogService,
            IMvxNavigationService navigationService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(loginManager, nameof(loginManager));
            Ensure.Argument.IsNotNull(dialogService, nameof(dialogService));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));

            this.dataSource = dataSource;
            this.loginManager = loginManager;
            this.dialogService = dialogService;
            this.navigationService = navigationService;

            DoneCommand = new MvxCommand(done);
            SignOutCommand = new MvxAsyncCommand(signout);
            TogglePasswordVisibilityCommand = new MvxCommand(togglePasswordVisibility);
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            needsSync = await dataSource.HasUnsyncedData();
            var user = await dataSource.User.Current();
            Email = user.Email;
        }

        private void togglePasswordVisibility()
            => IsPasswordMasked = !IsPasswordMasked;

        private async Task signout()
        {
            var shouldLogout = !needsSync || await dialogService.Confirm(
                Resources.AreYouSure,
                Resources.SettingsUnsyncedMessage,
                Resources.SettingsDialogButtonSignOut,
                Resources.Cancel
            );

            if (!shouldLogout) return;

            await dataSource.Logout();
            await navigationService.Navigate<OnboardingViewModel>();
        }

        private void done()
        {
            if (!NextIsEnabled) return;

            IsLoading = true;
            
            loginManager
                .RefreshToken(Password)
                .Subscribe(onDataSource, onError);
        }

        private async void onDataSource(ITogglDataSource newDataSource)
        {
            Mvx.RegisterSingleton(newDataSource);

            await newDataSource.SyncManager.ForceFullSync();

            IsLoading = false;

            await navigationService.Navigate<MainViewModel>();
        }

        private void onError(Exception ex)
        {
            IsLoading = false;
            Error = ex is ForbiddenException ? Resources.IncorrectPassword : Resources.GenericLoginError;
        }
    }
}
