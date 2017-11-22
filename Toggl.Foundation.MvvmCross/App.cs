﻿using MvvmCross.Core.Navigation;
using MvvmCross.Core.ViewModels;
using MvvmCross.Platform;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac;
using Toggl.PrimeRadiant;

namespace Toggl.Foundation.MvvmCross
{
    public sealed class App : MvxApplication
    {
        public override void Initialize()
        {

        }

        public void Initialize(ILoginManager loginManager, IMvxNavigationService navigationService, IAccessRestrictionStorage accessRestrictionStorage)
        {
            Ensure.Argument.IsNotNull(loginManager, nameof(loginManager));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(accessRestrictionStorage, nameof(accessRestrictionStorage));

            Mvx.RegisterSingleton<IPasswordManagerService>(new StubPasswordManagerService());

            RegisterAppStart(new AppStart(loginManager, navigationService, accessRestrictionStorage));
        }
    }

    public sealed class AppStart : IMvxAppStart
    {
        private readonly ILoginManager loginManager;
        private readonly IMvxNavigationService navigationService;
        private readonly IAccessRestrictionStorage accessRestrictionStorage;

        public AppStart(ILoginManager loginManager, IMvxNavigationService navigationService, IAccessRestrictionStorage accessRestrictionStorage)
        {
            Ensure.Argument.IsNotNull(loginManager, nameof(loginManager));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(accessRestrictionStorage, nameof(accessRestrictionStorage));

            this.loginManager = loginManager;
            this.navigationService = navigationService;
            this.accessRestrictionStorage = accessRestrictionStorage;
        }

        public void Start(object hint = null)
        {
            Mvx.RegisterSingleton(loginManager);

            if (accessRestrictionStorage.IsApiOutdated() || accessRestrictionStorage.IsClientOutdated())
            {
                navigationService.Navigate<OnboardingViewModel>(); // TODO: navigate user to the correct screen
                return;
            }

            if (accessRestrictionStorage.IsUnauthorized())
            {
                navigationService.Navigate<OnboardingViewModel>(); // TODO: navigate user to the correct screen
                return;
            }

            var dataSource = loginManager.GetDataSourceIfLoggedIn();
            if (dataSource == null)
            {
                navigationService.Navigate<OnboardingViewModel>();
                return;
            }

            dataSource.SyncManager.ForceFullSync();

            Mvx.RegisterSingleton(dataSource);
            navigationService.Navigate<MainViewModel>();
        }
    }
}
