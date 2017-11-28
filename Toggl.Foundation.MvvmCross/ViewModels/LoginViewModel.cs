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
using Toggl.PrimeRadiant;
using Toggl.Ultrawave.Exceptions;
using EmailType = Toggl.Multivac.Email;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class LoginViewModel : MvxViewModel<LoginType>
    {
        public const int EmailPage = 0;
        public const int PasswordPage = 1;
        public const int ForgotPasswordPage = 2;
        public const string PrivacyPolicyUrl = "https://toggl.com/legal/privacy";
        public const string TermsOfServiceUrl = "https://toggl.com/legal/terms";

        private readonly ILoginManager loginManager;
        private readonly IMvxNavigationService navigationService;
        private readonly IPasswordManagerService passwordManagerService;
        private readonly IAccessRestrictionStorage accessRestrictionStorage;

        private LoginType loginType;
        private IDisposable loginDisposable;
        private IDisposable passwordManagerDisposable;

        private EmailType email = EmailType.Invalid;

        private int pageBeforeForgotPasswordPage;

        public bool IsLogin => loginType == LoginType.Login;

        public bool IsSignUp => loginType == LoginType.SignUp;

        [DependsOn(nameof(IsLogin), nameof(IsForgotPasswordPage))]
        public string Title
        {
            get
            {
                if (IsSignUp)
                    return Resources.SignUpTitle;

                if (IsForgotPasswordPage)
                    return Resources.LoginForgotPassword;

                return Resources.LoginTitle;
            }
        }

        public string Email { get; set; } = "";

        public string Password { get; set; } = "";

        public string InfoText { get; set; } = "";

        [DependsOn(nameof(InfoText))]
        public bool HasInfoText => !string.IsNullOrEmpty(InfoText);

        public int CurrentPage { get; private set; } = EmailPage;

        public bool IsLoading { get; private set; } = false;

        public bool IsPasswordMasked { get; private set; } = true;

        public IMvxCommand NextCommand { get; }

        public IMvxCommand BackCommand { get; }

        public IMvxCommand ForgotPasswordCommand { get; }

        public IMvxCommand OpenPrivacyPolicyCommand { get; }

        public IMvxCommand OpenTermsOfServiceCommand { get; }

        public IMvxCommand TogglePasswordVisibilityCommand { get; }

        public IMvxCommand StartPasswordManagerCommand { get; }

        [DependsOn(nameof(CurrentPage))]
        public bool IsEmailPage => CurrentPage == EmailPage;

        [DependsOn(nameof(CurrentPage))]
        public bool IsPasswordPage => CurrentPage == PasswordPage;

        [DependsOn(nameof(CurrentPage))]
        public bool IsForgotPasswordPage => CurrentPage == ForgotPasswordPage;

        [DependsOn(nameof(IsEmailPage), nameof(IsForgotPasswordPage))]
        public bool EmailFieldVisible => IsEmailPage || IsForgotPasswordPage;

        [DependsOn(nameof(IsPasswordPage), nameof(IsLoading))]
        public bool ShowPasswordButtonVisible => IsPasswordPage && !IsLoading;

        [DependsOn(nameof(IsLogin), nameof(IsForgotPasswordPage))]
        public bool ShowForgotPassword => IsLogin && !IsForgotPasswordPage;

        [DependsOn(nameof(CurrentPage), nameof(Password))]
        public bool NextIsEnabled
        {
            get
            {
                if (IsEmailPage)
                    return email.IsValid;
                if (IsPasswordPage)
                    return Password.Length > 0 && !IsLoading;
                if (IsForgotPasswordPage)
                    return email.IsValid && !IsLoading;
                return false;
            }
        }

        public bool IsPasswordManagerAvailable
            => passwordManagerService.IsAvailable;

        public LoginViewModel(
            ILoginManager loginManager,
            IMvxNavigationService navigationService,
            IPasswordManagerService passwordManagerService,
            IAccessRestrictionStorage accessRestrictionStorage)
        {
            Ensure.Argument.IsNotNull(loginManager, nameof(loginManager));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(passwordManagerService, nameof(passwordManagerService));
            Ensure.Argument.IsNotNull(accessRestrictionStorage, nameof(accessRestrictionStorage));

            this.loginManager = loginManager;
            this.navigationService = navigationService;
            this.passwordManagerService = passwordManagerService;
            this.accessRestrictionStorage = accessRestrictionStorage;

            BackCommand = new MvxCommand(back);
            NextCommand = new MvxCommand(next);
            ForgotPasswordCommand = new MvxCommand(forgotPassword);
            StartPasswordManagerCommand = new MvxCommand(startPasswordManager);
            OpenPrivacyPolicyCommand = new MvxCommand(openPrivacyPolicyCommand);
            OpenTermsOfServiceCommand = new MvxCommand(openTermsOfServiceCommand);
            TogglePasswordVisibilityCommand = new MvxCommand(togglePasswordVisibility);
        }

        public override void Prepare(LoginType parameter)
        {
            loginType = parameter;
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

            if (IsForgotPasswordPage)
            {
                resetPassword();
                return;
            }

            CurrentPage = PasswordPage;
            InfoText = loginType == LoginType.SignUp
                ? Resources.SignUpPasswordRequirements
                : "";
        }

        private void resetPassword()
        {
            IsLoading = true;
            loginManager
                .ResetPassword(email)
                .Subscribe(onPasswordResetSuccess, onPasswordResetError);
        }

        private void onPasswordResetSuccess(string result)
        {
            IsLoading = false;
            CurrentPage = PasswordPage;
            InfoText = Resources.PasswordResetSuccess;
        }

        private void onPasswordResetError(Exception exception)
        {
            IsLoading = false;

            switch (exception)
            {
                case BadRequestException _:
                    InfoText = Resources.PasswordResetEmailDoesNotExistError;
                    break;

                case OfflineException _:
                    InfoText = Resources.PasswordResetOfflineError;
                    break;

                case ApiException apiException:
                    InfoText = apiException.LocalizedApiErrorMessage;
                    break;

                default:
                    InfoText = Resources.PasswordResetGeneralError;
                    break;
            }
        }

        private void back()
        {
            if (IsEmailPage)
                navigationService.Close(this);

            if (IsForgotPasswordPage)
                CurrentPage = pageBeforeForgotPasswordPage;
            else
                CurrentPage--;
            
            InfoText = "";
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

        private void onError(Exception exception)
        {
            IsLoading = false;
            onCompleted();

            switch (exception)
            {
                case ApiDeprecatedException apiDeprecated:
                    accessRestrictionStorage.SetApiOutdated();
                    navigationService.Navigate<OutdatedAppViewModel>();
                    return;
                case ClientDeprecatedException clientDeprecated:
                    accessRestrictionStorage.SetClientOutdated();
                    navigationService.Navigate<OutdatedAppViewModel>();
                    return;
                case UnauthorizedException forbidden:
                    InfoText = Resources.IncorrectEmailOrPassword;
                    break;
                default:
                    InfoText = getGenericError();
                    break;
            }
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

        private void forgotPassword()
        {
            pageBeforeForgotPasswordPage = CurrentPage;
            CurrentPage = ForgotPasswordPage;
            InfoText = email.IsValid ? "" : Resources.PasswordResetExplanation;
        }
    }
}
