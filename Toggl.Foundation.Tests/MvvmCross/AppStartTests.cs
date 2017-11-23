using System;
using System.Threading.Tasks;
using FluentAssertions;
using MvvmCross.Core.Navigation;
using NSubstitute;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.PrimeRadiant;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross
{
    public sealed class AppStartTests
    {
        public abstract class AppStartTest : BaseMvvmCrossTests
        {
            protected AppStart AppStart { get; }
            protected ILoginManager LoginManager { get; } = Substitute.For<ILoginManager>();
            protected IAccessRestrictionStorage AccessRestrictionStorage { get; } =
                Substitute.For<IAccessRestrictionStorage>();

            protected AppStartTest()
            {
                AppStart = new AppStart(LoginManager, NavigationService, AccessRestrictionStorage);
            }
        }

        public sealed class TheConstructor : AppStartTest
        {
            [Theory]
            [ClassData(typeof(ThreeParameterConstructorTestData))]
            public void ThrowsIfAnyOfTheArgumentsIsNull(bool userLoginManager, bool userNavigationService, bool useAccessRestrictionStorage)
            {
                var loginManager = userLoginManager ? LoginManager : null;
                var navigationService = userNavigationService ? NavigationService : null;
                var accessRestrictionStorage = useAccessRestrictionStorage ? AccessRestrictionStorage : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new AppStart(loginManager, navigationService, accessRestrictionStorage);

                tryingToConstructWithEmptyParameters
                    .ShouldThrow<ArgumentNullException>();
            }
        }

        public sealed class TheStartMethod : AppStartTest
        {
            [Fact]
            public void ShowsTheOutdatedViewIfTheCurrentVersionOfTheAppIsOutdated()
            {
                AccessRestrictionStorage.IsClientOutdated().Returns(true);

                AppStart.Start();
                Task.Delay(10).Wait();

                NavigationService.Received().Navigate<OnboardingViewModel>();
                NavigationService.Received().Navigate<OutdatedAppViewModel>();
                LoginManager.DidNotReceive().GetDataSourceIfLoggedIn();
            }

            [Fact]
            public void ShowsTheOutdatedViewIfTheVersionOfTheCurrentlyUsedApiIsOutdated()
            {
                AccessRestrictionStorage.IsApiOutdated().Returns(true);

                AppStart.Start();
                Task.Delay(10).Wait();

                NavigationService.Received().Navigate<OnboardingViewModel>();
                NavigationService.Received().Navigate<OutdatedAppViewModel>();
                LoginManager.DidNotReceive().GetDataSourceIfLoggedIn();
            }

            [Fact]
            public void ShowsTheReLoginViewIfTheUserRevokedTheApiToken()
            {
                AccessRestrictionStorage.IsUnauthorized().Returns(true);

                AppStart.Start();

                NavigationService.Received().Navigate<LoginViewModel>(); // TODO: use repeat login view model when it is ready
                LoginManager.DidNotReceive().GetDataSourceIfLoggedIn();
            }

            [Fact]
            public void ShowsTheOutdatedViewIfTheTokenWasRevokedAndTheAppIsOutdated()
            {
                AccessRestrictionStorage.IsUnauthorized().Returns(true);
                AccessRestrictionStorage.IsClientOutdated().Returns(true);

                AppStart.Start();
                Task.Delay(10).Wait();

                NavigationService.Received().Navigate<OnboardingViewModel>();
                NavigationService.Received().Navigate<OutdatedAppViewModel>();
                LoginManager.DidNotReceive().GetDataSourceIfLoggedIn();
            }

            [Fact]
            public void ShowsTheOutdatedViewIfTheTokenWasRevokedAndTheApiIsOutdated()
            {
                AccessRestrictionStorage.IsUnauthorized().Returns(true);
                AccessRestrictionStorage.IsApiOutdated().Returns(true);

                AppStart.Start();
                Task.Delay(10).Wait();

                NavigationService.Received().Navigate<OnboardingViewModel>();
                NavigationService.Received().Navigate<OutdatedAppViewModel>();
                NavigationService.DidNotReceive().Navigate<LoginViewModel>(); // TODO: use repeat login view model when it is ready
                LoginManager.DidNotReceive().GetDataSourceIfLoggedIn();
            }

            [Fact]
            public void ShowsTheOnboardingViewModelIfTheUserHasNotLoggedInPreviously()
            {
                ITogglDataSource dataSource = null;
                LoginManager.GetDataSourceIfLoggedIn().Returns(dataSource);

                AppStart.Start();

                NavigationService.Received().Navigate(typeof(OnboardingViewModel));
            }

            [Fact]
            public void ShowsTheTimeEntriesViewModelIfTheUserHasLoggedInPreviously()
            {
                var dataSource = Substitute.For<ITogglDataSource>();
                LoginManager.GetDataSourceIfLoggedIn().Returns(dataSource);

                AppStart.Start();

                NavigationService.Received().Navigate(typeof(MainViewModel));
            }
        }
    }
}
