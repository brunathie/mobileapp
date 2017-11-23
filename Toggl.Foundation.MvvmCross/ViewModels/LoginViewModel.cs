using System;
using System.Reactive.Linq;
using MvvmCross.Core.Navigation;
using MvvmCross.Core.ViewModels;
using MvvmCross.Platform;
using PropertyChanged;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Multivac;
using Toggl.Ultrawave.Exceptions;
using EmailType = Toggl.Multivac.Email;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class LoginViewModel : MvxViewModel<LoginType>
    {
        public const int EmailPage = 0;
        public const int PasswordPage = 1;
        public const string PrivacyPolicyUrl = "https://toggl.com/legal/privacy";
        public const string TermsOfServiceUrl = "https://toggl.com/legal/terms";

        private readonly ILoginManager loginManager;
        private readonly IMvxNavigationService navigationService;
        private readonly IPasswordManagerService passwordManagerService;

        private LoginType loginType;
        private IDisposable loginDisposable;
        private IDisposable passwordManagerDisposable;

        private EmailType email = EmailType.Invalid;

        public bool IsLogin => loginType == LoginType.Login;

        public bool IsSignUp => loginType == LoginType.SignUp;

        public string Title { get; private set; }

        public string Email { get; set; } = "";

        public string Password { get; set; } = "";

        public string ErrorText { get; set; } = "";

        [DependsOn(nameof(ErrorText))]
        public bool HasError => !string.IsNullOrEmpty(ErrorText);

        public int CurrentPage { get; private set; } = EmailPage;

        public bool IsLoading { get; private set; } = false;

        public bool IsPasswordMasked { get; private set; } = true;

        public IMvxCommand NextCommand { get; }

        public IMvxCommand BackCommand { get; }

        public IMvxCommand OpenPrivacyPolicyCommand { get; }

        public IMvxCommand OpenTermsOfServiceCommand { get; }

        public IMvxCommand TogglePasswordVisibilityCommand { get; }

        public IMvxCommand StartPasswordManagerCommand { get; }

        [DependsOn(nameof(CurrentPage))]
        public bool IsEmailPage => CurrentPage == EmailPage;

        [DependsOn(nameof(CurrentPage))]
        public bool IsPasswordPage => CurrentPage == PasswordPage;

        [DependsOn(nameof(IsPasswordPage), nameof(IsLoading))]
        public bool ShowPasswordButtonVisible => IsPasswordPage && !IsLoading;

        [DependsOn(nameof(IsLogin), nameof(IsPasswordPage))]
        public bool ShowForgotPassword => IsLogin && IsPasswordPage;

        [DependsOn(nameof(CurrentPage), nameof(Password))]
        public bool NextIsEnabled
            => IsEmailPage ? email.IsValid : (Password.Length > 0 && !IsLoading);

        public bool IsPasswordManagerAvailable
            => passwordManagerService.IsAvailable;

        public LoginViewModel(ILoginManager loginManager, IMvxNavigationService navigationService, IPasswordManagerService passwordManagerService)
        {
            Ensure.Argument.IsNotNull(loginManager, nameof(loginManager));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(passwordManagerService, nameof(passwordManagerService));

            this.loginManager = loginManager;
            this.navigationService = navigationService;
            this.passwordManagerService = passwordManagerService;

            BackCommand = new MvxCommand(back);
            NextCommand = new MvxCommand(next);
            StartPasswordManagerCommand = new MvxCommand(startPasswordManager);
            OpenPrivacyPolicyCommand = new MvxCommand(openPrivacyPolicyCommand);
            OpenTermsOfServiceCommand = new MvxCommand(openTermsOfServiceCommand);
            TogglePasswordVisibilityCommand = new MvxCommand(togglePasswordVisibility);
        }

        public override void Prepare(LoginType parameter)
        {
            loginType = parameter;
            Title = loginType == LoginType.Login ? Resources.LoginTitle : Resources.SignUpTitle;
        }

        private void OnEmailChanged()
        {
            email = EmailType.FromString(Email);
            RaisePropertyChanged(nameof(NextIsEnabled));
        }

        private void openTermsOfServiceCommand() =>
            navigationService.Navigate<BrowserViewModel, BrowserParameters>(
                BrowserParameters.WithUrlAndTitle(TermsOfServiceUrl, Resources.TermsOfService)
            );

        private void openPrivacyPolicyCommand() =>
            navigationService.Navigate<BrowserViewModel, BrowserParameters>(
                BrowserParameters.WithUrlAndTitle(PrivacyPolicyUrl, Resources.PrivacyPolicy)
            );

        private void next()
        {
            if (!NextIsEnabled) return;

            if (IsPasswordPage)
            {
                if (IsLogin) login();
                if (IsSignUp) signUp();
            }


            CurrentPage = PasswordPage;
            ErrorText = "";
        }

        private void back()
        {
            if (IsEmailPage)
                navigationService.Close(this);

            CurrentPage = EmailPage;
            ErrorText = "";
        }

        private void togglePasswordVisibility()
            => IsPasswordMasked = !IsPasswordMasked;

        private void login()
        {
            IsLoading = true;

            loginDisposable =
                loginManager
                    .Login(email, Password)
                    .Subscribe(onDataSource, onError, onCompleted);
        }

        private void signUp()
        {
            IsLoading = true;

            loginDisposable =
                loginManager
                    .SignUp(email, Password)
                    .Subscribe(onDataSource, onError, onCompleted);
        }

        private void startPasswordManager()
        {
            if (!passwordManagerService.IsAvailable) return;
            if (passwordManagerDisposable != null) return;

            passwordManagerDisposable =
                passwordManagerService
                    .GetLoginInformation()
                    .Subscribe(onLoginInfo, onError, onCompleted);
        }

        private void onLoginInfo(PasswordManagerResult loginInfo)
        {
            Email = loginInfo.Email;
            if (!NextIsEnabled) return;

            CurrentPage = PasswordPage;
            Password = loginInfo.Password;
            if (!NextIsEnabled) return;

            login();
        }

        private async void onDataSource(ITogglDataSource dataSource)
        {
            Mvx.RegisterSingleton(dataSource);

            await dataSource.SyncManager.ForceFullSync();

            IsLoading = false;

            await navigationService.Navigate<MainViewModel>();
        }

        private void onError(Exception ex)
        {
            ErrorText = ex is ForbiddenException ? Resources.IncorrectEmailOrPassword
                                                 : getGenericError();

            IsLoading = false;
            onCompleted();
        }

        private string getGenericError()
            => loginType == LoginType.Login
                ? Resources.GenericLoginError
                : Resources.GenericSignUpError;

        private void onCompleted()
        {
            loginDisposable?.Dispose();
            passwordManagerDisposable?.Dispose();

            loginDisposable = null;
            passwordManagerDisposable = null;
        }
    }
}
