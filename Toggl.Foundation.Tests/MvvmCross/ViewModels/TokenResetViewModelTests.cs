using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public class TokenResetViewModelTests
    {
        public abstract class TokenResetViewModelTest : BaseViewModelTests<TokenResetViewModel>
        {
            protected const string ValidEmail = "susancalvin@psychohistorian.museum";
            protected const string InvalidEmail = "foo@";

            protected const string ValidPassword = "123456";
            protected const string InvalidPassword = "";

            protected ILoginManager LoginManager { get; } = Substitute.For<ILoginManager>();

            protected override TokenResetViewModel CreateViewModel()
                => new TokenResetViewModel(LoginManager, DataSource, DialogService, NavigationService);
        }

        public sealed class TheConstructor : TokenResetViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ClassData(typeof(FourParameterConstructorTestData))]
            public void ThrowsIfAnyOfTheArgumentsIsNull(bool userLoginManager,
                bool userNavigationService, bool useDataSource, bool useDialogService)
            {
                var loginManager = userLoginManager ? LoginManager : null;
                var navigationService = userNavigationService ? NavigationService : null;
                var dataSource = useDataSource ? DataSource : null;
                var dialogService = useDialogService ? DialogService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new TokenResetViewModel(loginManager, dataSource, dialogService, navigationService);

                tryingToConstructWithEmptyParameters
                    .ShouldThrow<ArgumentNullException>();
            }
        }

        public sealed class TheNextIsEnabledProperty : TokenResetViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void ReturnsFalseWhenThePasswordIsNotValid()
            {
                ViewModel.Password = InvalidPassword;

                ViewModel.NextIsEnabled.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsTrueIfThePasswordIsValid()
            {
                ViewModel.Password = ValidPassword;

                ViewModel.NextIsEnabled.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void ReturnsFalseWheThePasswordIsValidButTheViewIsLoading()
            {
                var scheduler = new TestScheduler();
                var never = Observable.Never<ITogglDataSource>();
                LoginManager.RefreshToken(Arg.Any<string>()).Returns(never);
                ViewModel.Password = ValidPassword;

                ViewModel.DoneCommand.Execute();

                ViewModel.NextIsEnabled.Should().BeFalse();
            }
        }

        public sealed class TheDoneCommand : TokenResetViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void DoesNotAttemptToLoginWhileThePasswordIsNotValid()
            {
                ViewModel.Password = InvalidPassword;

                ViewModel.DoneCommand.Execute();

                LoginManager.DidNotReceive().RefreshToken(Arg.Any<string>());
            }

            [Fact, LogIfTooSlow]
            public void CallsTheLoginManagerWhenThePasswordIsValid()
            {
                ViewModel.Password = ValidPassword;

                ViewModel.DoneCommand.Execute();

                LoginManager.Received().RefreshToken(Arg.Is(ValidPassword));
            }

            [Fact, LogIfTooSlow]
            public void NavigatesToTheMainViewModelModelWhenTheTokenRefreshSucceeds()
            {
                ViewModel.Password = ValidPassword;
                LoginManager.RefreshToken(Arg.Any<string>())
                            .Returns(Observable.Return(Substitute.For<ITogglDataSource>()));

                ViewModel.DoneCommand.Execute();

                NavigationService.Received().Navigate(typeof(MainViewModel));
            }

            [Fact, LogIfTooSlow]
            public void StopsTheViewModelLoadStateWhenItCompletes()
            {
                ViewModel.Password = ValidPassword;
                LoginManager.RefreshToken(Arg.Any<string>())
                            .Returns(Observable.Return(Substitute.For<ITogglDataSource>()));

                ViewModel.DoneCommand.Execute();

                ViewModel.IsLoading.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void StopsTheViewModelLoadStateWhenItErrors()
            {
                ViewModel.Password = ValidPassword;
                LoginManager.RefreshToken(Arg.Any<string>())
                            .Returns(Observable.Throw<ITogglDataSource>(new Exception()));

                ViewModel.DoneCommand.Execute();

                ViewModel.IsLoading.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void DoesNotNavigateWhenTheLoginFails()
            {
                ViewModel.Password = ValidPassword;
                LoginManager.RefreshToken(Arg.Any<string>())
                            .Returns(Observable.Throw<ITogglDataSource>(new Exception()));

                ViewModel.DoneCommand.Execute();

                NavigationService.DidNotReceive().Navigate(typeof(MainViewModel));
            }
        }

        public sealed class TheSignOutCommand : TokenResetViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void LogsTheUserOut()
            {
                ViewModel.SignOutCommand.Execute();

                DataSource.Received().Logout();
            }

            [Fact, LogIfTooSlow]
            public void NavigatesToTheOnboardingViewModel()
            {
                ViewModel.SignOutCommand.Execute();

                NavigationService.Received().Navigate(typeof(OnboardingViewModel));
            }

            [Fact, LogIfTooSlow]
            public async Task AsksForPermissionIfThereIsUnsyncedData()
            {
                DataSource.HasUnsyncedData().Returns(Observable.Return(true));
                await ViewModel.Initialize();

                ViewModel.SignOutCommand.Execute();

                await DialogService.Received().Confirm(
                    Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotLogTheUserOutIfPermissionIsDenied()
            {
                DialogService.Confirm(
                    Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                    .Returns(Task.FromResult(false));
                DataSource.HasUnsyncedData().Returns(Observable.Return(true));
                await ViewModel.Initialize();

                ViewModel.SignOutCommand.Execute();

                await DataSource.DidNotReceive().Logout();
            }
        }
    }
}
