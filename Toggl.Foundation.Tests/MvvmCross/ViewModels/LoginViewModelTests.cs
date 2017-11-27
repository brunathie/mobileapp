﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.TestExtensions;
using Toggl.Multivac;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;
using Xunit;
using MvvmCross.Core.Navigation;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class LoginViewModelTests
    {
        public abstract class LoginViewModelTest : BaseViewModelTests<LoginViewModel>
        {
            protected const string ValidEmail = "susancalvin@psychohistorian.museum";
            protected const string InvalidEmail = "foo@";

            protected const string ValidPassword = "123456";
            protected const string InvalidPassword = "";

            protected ILoginManager LoginManager { get; } = Substitute.For<ILoginManager>();
            protected IPasswordManagerService PasswordManagerService { get; } = Substitute.For<IPasswordManagerService>();

            protected IAccessRestrictionStorage AccessRestrictionStorage { get; } =
                Substitute.For<IAccessRestrictionStorage>();

            protected override LoginViewModel CreateViewModel()
                => new LoginViewModel(LoginManager, NavigationService, PasswordManagerService, AccessRestrictionStorage);
        }

        public sealed class TheConstructor : LoginViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ClassData(typeof(FourParameterConstructorTestData))]
            public void ThrowsIfAnyOfTheArgumentsIsNull(bool userLoginManager, bool userNavigationService, bool usePasswordManagerService, bool useAccessRestrictionStorage)
            {
                var loginManager = userLoginManager ? LoginManager : null;
                var navigationService = userNavigationService ? NavigationService : null;
                var passwordManagerService = usePasswordManagerService ? PasswordManagerService : null;
                var accessRestrictionStorage = useAccessRestrictionStorage ? AccessRestrictionStorage : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new LoginViewModel(loginManager, navigationService, passwordManagerService, accessRestrictionStorage);

                tryingToConstructWithEmptyParameters
                    .ShouldThrow<ArgumentNullException>();
            }
        }

        public sealed class TheNextIsEnabledProperty
        {
            public sealed class WhenInTheEmailPage : LoginViewModelTest
            {
                [Fact, LogIfTooSlow]
                public void ReturnsFalseIfTheEmailIsInvalid()
                {
                    ViewModel.Email = InvalidEmail;

                    ViewModel.NextIsEnabled.Should().BeFalse();
                }

                [Fact, LogIfTooSlow]
                public void ReturnsTrueIfTheEmailIsValid()
                {
                    ViewModel.Email = ValidEmail;

                    ViewModel.NextIsEnabled.Should().BeTrue();
                }
            }

            public sealed class WhenInThePasswordPage : LoginViewModelTest
            {
                public WhenInThePasswordPage()
                {
                    ViewModel.Email = ValidEmail;
                    ViewModel.NextCommand.Execute();
                }

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
                    LoginManager.Login(Arg.Any<Email>(), Arg.Any<string>()).Returns(never);
                    ViewModel.Password = ValidPassword;
                    ViewModel.NextCommand.Execute();

                    ViewModel.NextCommand.Execute();

                    ViewModel.NextIsEnabled.Should().BeFalse();
                }
            }
        }

        public sealed class ThePrepareMethod : LoginViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void SetsTheLoginType()
            {
                var parameter = LoginType.SignUp;

                ViewModel.Prepare(parameter);

                ViewModel.IsSignUp.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void SetsTheTitleToLoginWhenThePassedParameterIsLogin()
            {
                var parameter = LoginType.Login;

                ViewModel.Prepare(parameter);

                ViewModel.Title.Should().Be(Resources.LoginTitle);
            }

            [Fact, LogIfTooSlow]
            public void SetsTheTitleToSignupWhenThePassedParameterIsLogin()
            {
                var parameter = LoginType.SignUp;

                ViewModel.Prepare(parameter);

                ViewModel.Title.Should().Be(Resources.SignUpTitle);
            }
        }

        public sealed class TheNextCommand
        {
            public sealed class WhenInTheEmailPage : LoginViewModelTest
            {
                [Fact, LogIfTooSlow]
                public void DoesNothingWhenTheEmailIsInvalid()
                {
                    ViewModel.Email = InvalidEmail;

                    ViewModel.NextCommand.Execute();

                    ViewModel.CurrentPage.Should().Be(LoginViewModel.EmailPage);
                }

                [Fact, LogIfTooSlow]
                public void ShowsThePasswordPageWhenTheEmailIsValid()
                {
                    ViewModel.Email = ValidEmail;

                    ViewModel.NextCommand.Execute();

                    ViewModel.CurrentPage.Should().Be(LoginViewModel.PasswordPage);
                }
            }

            public sealed class WhenInThePasswordPage : LoginViewModelTest
            {
                public WhenInThePasswordPage()
                {
                    ViewModel.Email = ValidEmail;
                    ViewModel.NextCommand.Execute();
                }

                [Fact, LogIfTooSlow]
                public void DoesNotAttemptToLoginWhileThePasswordIsValid()
                {
                    ViewModel.Password = InvalidPassword;

                    ViewModel.NextCommand.Execute();

                    LoginManager.DidNotReceive().Login(Arg.Any<Email>(), Arg.Any<string>());
                }

                [Fact, LogIfTooSlow]
                public void CallsTheLoginManagerWhenThePasswordIsValid()
                {
                    ViewModel.Password = ValidPassword;

                    ViewModel.NextCommand.Execute();

                    LoginManager.Received().Login(Arg.Any<Email>(), Arg.Any<string>());
                }

                [Fact, LogIfTooSlow]
                public void CallsTheLoginManagerForSignUpWhenThePasswordIsValidAndInSignUpMode()
                {
                    ViewModel.Prepare(LoginType.SignUp);
                    ViewModel.Password = ValidPassword;

                    ViewModel.NextCommand.Execute();

                    LoginManager.Received().SignUp(Arg.Any<Email>(), Arg.Any<string>());
                }

                [Fact, LogIfTooSlow]
                public void DoesNothingWhenThePageIsCurrentlyLoading()
                {
                    var scheduler = new TestScheduler();
                    var never = Observable.Never<ITogglDataSource>();
                    LoginManager.Login(Arg.Any<Email>(), Arg.Any<string>()).Returns(never);

                    ViewModel.Password = ValidPassword;
                    ViewModel.NextCommand.Execute();

                    ViewModel.NextCommand.Execute();

                    LoginManager.Received(1).Login(Arg.Any<Email>(), Arg.Any<string>());
                }

                [Fact, LogIfTooSlow]
                public void NavigatesToTheTimeEntriesViewModelWhenTheLoginSucceeds()
                {
                    ViewModel.Password = ValidPassword;
                    LoginManager.Login(Arg.Any<Email>(), Arg.Any<string>())
                                .Returns(Observable.Return(Substitute.For<ITogglDataSource>()));

                    ViewModel.NextCommand.Execute();

                    NavigationService.Received().Navigate(typeof(MainViewModel));
                }

                [Fact, LogIfTooSlow]
                public void StopsTheViewModelLoadStateWhenItCompletes()
                {
                    ViewModel.Password = ValidPassword;
                    LoginManager.Login(Arg.Any<Email>(), Arg.Any<string>())
                                .Returns(Observable.Return(Substitute.For<ITogglDataSource>()));

                    ViewModel.NextCommand.Execute();

                    ViewModel.IsLoading.Should().BeFalse();
                }

                [Fact, LogIfTooSlow]
                public void StopsTheViewModelLoadStateWhenItErrors()
                {
                    ViewModel.Password = ValidPassword;
                    LoginManager.Login(Arg.Any<Email>(), Arg.Any<string>())
                                .Returns(Observable.Throw<ITogglDataSource>(new Exception()));

                    ViewModel.NextCommand.Execute();

                    ViewModel.IsLoading.Should().BeFalse();
                }

                [Fact, LogIfTooSlow]
                public void DoesNotNavigateWhenTheLoginFails()
                {
                    ViewModel.Password = ValidPassword;
                    LoginManager.Login(Arg.Any<Email>(), Arg.Any<string>())
                                .Returns(Observable.Throw<ITogglDataSource>(new Exception()));

                    ViewModel.NextCommand.Execute();

                    NavigationService.DidNotReceive().Navigate(typeof(MainViewModel));
                }
            }
        }

        public sealed class TheTermsOfServiceCommand : LoginViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void OpensTheBrowserInTheTermsOfServicePage()
            {
                ViewModel.OpenTermsOfServiceCommand.Execute();

                NavigationService.Received().Navigate(
                    typeof(BrowserViewModel),
                    Arg.Is<BrowserParameters>(parameter => parameter.Url == LoginViewModel.TermsOfServiceUrl)
                );
            }

            [Fact, LogIfTooSlow]
            public void OpensTheBrowserWithTheAppropriateTitle()
            {
                ViewModel.OpenTermsOfServiceCommand.Execute();

                NavigationService.Received().Navigate(
                    typeof(BrowserViewModel),
                    Arg.Is<BrowserParameters>(parameter => parameter.Title == Resources.TermsOfService)
                );
            }
        }

        public sealed class ThePrivacyPolicyCommand : LoginViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void OpensTheBrowserInThePrivacyPolicyPage()
            {
                ViewModel.OpenPrivacyPolicyCommand.Execute();

                NavigationService.Received().Navigate(
                    typeof(BrowserViewModel),
                    Arg.Is<BrowserParameters>(parameter => parameter.Url == LoginViewModel.PrivacyPolicyUrl)
                );
            }

            [Fact, LogIfTooSlow]
            public void OpensTheBrowserWithTheAppropriateTitle()
            {
                ViewModel.OpenPrivacyPolicyCommand.Execute();

                NavigationService.Received().Navigate(
                    typeof(BrowserViewModel),
                    Arg.Is<BrowserParameters>(parameter => parameter.Title == Resources.PrivacyPolicy)
                );
            }
        }

        public sealed class ThePreviousCommand : LoginViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void ReturnsToTheEmailPage()
            {
                ViewModel.Email = ValidEmail;
                ViewModel.NextCommand.Execute();

                ViewModel.BackCommand.Execute();

                ViewModel.CurrentPage.Should().Be(LoginViewModel.EmailPage);
            }

            [Fact, LogIfTooSlow]
            public void ClosesTheViewModelWhenInTheEmailPage()
            {
                ViewModel.BackCommand.Execute();

                NavigationService.Received().Close(Arg.Is(ViewModel));
            }
        }

        public sealed class TheStartPasswordManagerCommandCommand : LoginViewModelTest
        {
            public TheStartPasswordManagerCommandCommand()
            {
                PasswordManagerService.IsAvailable.Returns(true);
            }

            [Fact, LogIfTooSlow]
            public void DoesNotTryToCallThePasswordManagerServiceIfItIsNotAvailable()
            {
                PasswordManagerService.IsAvailable.Returns(false);

                ViewModel.StartPasswordManagerCommand.Execute();

                PasswordManagerService.DidNotReceive().GetLoginInformation();
            }

            [Fact, LogIfTooSlow]
            public void CallsThePasswordManagerServiceWhenTheServiceIsAvailable()
            {
                PasswordManagerService.GetLoginInformation().Returns(Observable.Never<PasswordManagerResult>());

                ViewModel.StartPasswordManagerCommand.Execute();

                PasswordManagerService.Received().GetLoginInformation();
            }

            [Fact, LogIfTooSlow]
            public void DoesNothingWhenCalledASecondTimeBeforeTheObservableFromTheFirstCallReturns()
            {
                var scheduler = new TestScheduler();
                var never = Observable.Never<PasswordManagerResult>();
                PasswordManagerService.GetLoginInformation().Returns(never);

                scheduler.Schedule(TimeSpan.FromTicks(20), () => ViewModel.StartPasswordManagerCommand.Execute());
                scheduler.Schedule(TimeSpan.FromTicks(40), () => ViewModel.StartPasswordManagerCommand.Execute());

                scheduler.Start();

                PasswordManagerService.Received(1).GetLoginInformation();
            }

            [Fact, LogIfTooSlow]
            public void CallsTheLoginCommandWhenValidCredentialsAreProvided()
            {
                var scheduler = new TestScheduler();
                var observable = arrangeCallToPasswordManagerWithValidCredentials();

                scheduler.Schedule(TimeSpan.FromTicks(20), () => ViewModel.StartPasswordManagerCommand.Execute());

                scheduler.Start(
                    () => observable,
                    created: 0,
                    subscribed: 10,
                    disposed: 100
                );

                LoginManager.Received().Login(Arg.Any<Email>(), Arg.Any<string>());
            }

            [Fact, LogIfTooSlow]
            public void SetsTheEmailFieldWhenValidCredentialsAreProvided()
            {
                var scheduler = new TestScheduler();
                var observable = arrangeCallToPasswordManagerWithValidCredentials();

                scheduler.Schedule(TimeSpan.FromTicks(20), () => ViewModel.StartPasswordManagerCommand.Execute());

                scheduler.Start(
                    () => observable,
                    created: 0,
                    subscribed: 10,
                    disposed: 100
                );

                ViewModel.Email.Should().Be(ValidEmail);
            }

            [Fact, LogIfTooSlow]
            public void SetsTheEmailFieldWhenInvalidCredentialsAreProvided()
            {
                var scheduler = new TestScheduler();
                var observable = arrangeCallToPasswordManagerWithInvalidCredentials();

                scheduler.Schedule(TimeSpan.FromTicks(20), () => ViewModel.StartPasswordManagerCommand.Execute());

                scheduler.Start(
                    () => observable,
                    created: 0,
                    subscribed: 10,
                    disposed: 100
                );

                ViewModel.Email.Should().Be(InvalidEmail);
            }

            [Fact, LogIfTooSlow]
            public void SetsThePasswordFieldWhenValidCredentialsAreProvided()
            {
                var scheduler = new TestScheduler();
                var observable = arrangeCallToPasswordManagerWithValidCredentials();

                scheduler.Schedule(TimeSpan.FromTicks(20), () => ViewModel.StartPasswordManagerCommand.Execute());

                scheduler.Start(
                    () => observable,
                    created: 0,
                    subscribed: 10,
                    disposed: 100
                );

                ViewModel.Password.Should().Be(ValidPassword);
            }

            [Fact, LogIfTooSlow]
            public void DoesNotSetThePasswordFieldWhenInvalidCredentialsAreProvided()
            {
                var scheduler = new TestScheduler();
                var observable = arrangeCallToPasswordManagerWithInvalidCredentials();

                scheduler.Schedule(TimeSpan.FromTicks(20), () => ViewModel.StartPasswordManagerCommand.Execute());

                scheduler.Start(
                    () => observable,
                    created: 0,
                    subscribed: 10,
                    disposed: 100
                );

                ViewModel.Password.Should().Be("");
            }

            [Fact, LogIfTooSlow]
            public void DoesNothingWhenValidCredentialsAreNotProvided()
            {
                var scheduler = new TestScheduler();
                var observable = arrangeCallToPasswordManagerWithInvalidCredentials();

                scheduler.Schedule(TimeSpan.FromTicks(20), () => ViewModel.StartPasswordManagerCommand.Execute());

                scheduler.Start(
                    () => observable,
                    created: 0,
                    subscribed: 10,
                    disposed: 100
                );

                LoginManager.DidNotReceive().Login(Arg.Any<Email>(), Arg.Any<string>());
            }

            private IObservable<PasswordManagerResult> arrangeCallToPasswordManagerWithValidCredentials()
            {
                var loginInfo = new PasswordManagerResult(ValidEmail, ValidPassword);
                var observable = Observable.Return(loginInfo);
                PasswordManagerService.GetLoginInformation().Returns(observable);

                return observable;
            }

            private IObservable<PasswordManagerResult> arrangeCallToPasswordManagerWithInvalidCredentials()
            {
                var loginInfo = new PasswordManagerResult(InvalidEmail, InvalidPassword);
                var observable = Observable.Return(loginInfo);
                PasswordManagerService.GetLoginInformation().Returns(observable);

                return observable;
            }
        }

        public sealed class TheHasErrorProperty : LoginViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void IsFalseWhenLoginSucceeds()
            {
                LoginManager.Login(Arg.Any<Email>(), Arg.Any<string>())
                            .Returns(Observable.Return(DataSource));
                ViewModel.Email = ValidEmail;
                ViewModel.NextCommand.Execute();
                ViewModel.Password = ValidPassword;

                ViewModel.NextCommand.Execute();

                ViewModel.HasError.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void IsTrueWhenLoginFails()
            {
                var scheduler = new TestScheduler();
                var forbiddenException = new ForbiddenException(Substitute.For<IRequest>(), Substitute.For<IResponse>());
                var notification = Notification.CreateOnError<ITogglDataSource>(forbiddenException);
                var message = new Recorded<Notification<ITogglDataSource>>(0, notification);
                var observable = scheduler.CreateColdObservable(message);
                LoginManager.Login(Arg.Any<Email>(), Arg.Any<string>())
                            .Returns(observable);
                ViewModel.Email = ValidEmail;
                ViewModel.NextCommand.Execute();
                ViewModel.Password = ValidPassword;

                ViewModel.NextCommand.Execute();
                scheduler.AdvanceTo(1);

                ViewModel.HasError.Should().BeTrue();
            }
        }

        public sealed class TheErrorTextProperty : LoginViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void IsEmptyWhenLoginSucceeds()
            {
                LoginManager.Login(Arg.Any<Email>(), Arg.Any<string>())
                            .Returns(Observable.Return(DataSource));
                ViewModel.Email = ValidEmail;
                ViewModel.NextCommand.Execute();
                ViewModel.Password = ValidPassword;

                ViewModel.NextCommand.Execute();

                ViewModel.ErrorText.Should().Be("");
            }

            [Fact, LogIfTooSlow]
            public void IsWrongPasswordErrorWhenForbiddenExceptionIsThrown()
            {
                var scheduler = new TestScheduler();
                var forbiddenException = new ForbiddenException(Substitute.For<IRequest>(), Substitute.For<IResponse>());
                var notification = Notification.CreateOnError<ITogglDataSource>(forbiddenException);
                var message = new Recorded<Notification<ITogglDataSource>>(0, notification);
                var observable = scheduler.CreateColdObservable(message);
                LoginManager.Login(Arg.Any<Email>(), Arg.Any<string>())
                            .Returns(observable);
                ViewModel.Email = ValidEmail;
                ViewModel.NextCommand.Execute();
                ViewModel.Password = ValidPassword;

                ViewModel.NextCommand.Execute();
                scheduler.AdvanceTo(1);

                ViewModel.ErrorText.Should().Be(Resources.IncorrectEmailOrPassword);
            }

            [Fact, LogIfTooSlow]
            public void IsGenericLoginErrorWhenAnyOtherExceptionIsThrown()
            {
                var scheduler = new TestScheduler();
                var notification = Notification.CreateOnError<ITogglDataSource>(new Exception());
                var message = new Recorded<Notification<ITogglDataSource>>(0, notification);
                var observable = scheduler.CreateColdObservable(message);
                LoginManager.Login(Arg.Any<Email>(), Arg.Any<string>())
                            .Returns(observable);
                ViewModel.Email = ValidEmail;
                ViewModel.NextCommand.Execute();
                ViewModel.Password = ValidPassword;

                ViewModel.NextCommand.Execute();
                scheduler.AdvanceTo(1);

                ViewModel.ErrorText.Should().Be(Resources.GenericLoginError);
            }

            [Fact, LogIfTooSlow]
            public void IsPasswordRequirementsWhenSwitchingFromEmailToPasswordPageInSignUpMode()
            {
                ViewModel.Prepare(LoginType.SignUp);
                ViewModel.Email = ValidEmail;

                ViewModel.NextCommand.Execute();

                ViewModel.ErrorText.Should().Be(Resources.SignUpPasswordRequirements);
            }

            [Fact, LogIfTooSlow]
            public void IsEmptyWhenSwitchingFromEmailToPasswordPageInLoginMode()
            {
                ViewModel.Prepare(LoginType.Login);
                ViewModel.Email = ValidEmail;

                ViewModel.NextCommand.Execute();

                ViewModel.ErrorText.Should().Be("");
            }
             
            [Fact, LogIfTooSlow]
            public void IsGenericSignUpErrorWhenAnyOtherExceptionIsThrown()
            {
                var scheduler = new TestScheduler();
                var notification = Notification.CreateOnError<ITogglDataSource>(new Exception());
                var message = new Recorded<Notification<ITogglDataSource>>(0, notification);
                var observable = scheduler.CreateColdObservable(message);
                LoginManager.SignUp(Arg.Any<Email>(), Arg.Any<string>())
                            .Returns(observable);
                ViewModel.Prepare(LoginType.SignUp);
                ViewModel.Email = ValidEmail;
                ViewModel.NextCommand.Execute();
                ViewModel.Password = ValidPassword;

                ViewModel.NextCommand.Execute();
                scheduler.AdvanceTo(1);

                ViewModel.ErrorText.Should().Be(Resources.GenericSignUpError);
            }
        }

        public sealed class ApiErrorHandling
        {
            public abstract class BaseApiErrorHandlingTests : LoginViewModelTest
            {
                private IRequest request => Substitute.For<IRequest>();
                private IResponse response => Substitute.For<IResponse>();

                [Fact, LogIfTooSlow]
                public void SetsTheOutdatedClientVersionFlag()
                {
                    CallAndThrow(new ClientDeprecatedException(request, response));

                    AccessRestrictionStorage.Received().SetClientOutdated();
                }

                [Fact, LogIfTooSlow]
                public void SetsTheOutdatedApiVersionFlag()
                {
                    CallAndThrow(new ApiDeprecatedException(request, response));

                    AccessRestrictionStorage.Received().SetApiOutdated();
                }

                [Fact, LogIfTooSlow]
                public void NavigatesToTheOutdatedClientScreen()
                {
                    CallAndThrow(new ClientDeprecatedException(request, response));

                    NavigationService.Received().Navigate<OutdatedAppViewModel>();
                }

                [Fact, LogIfTooSlow]
                public void NavigatesToTheOutdatedApiScreen()
                {
                    CallAndThrow(new ApiDeprecatedException(request, response));

                    NavigationService.Received().Navigate<OutdatedAppViewModel>();
                }

                protected abstract void CallAndThrow(Exception e);
            }

            public sealed class Login : BaseApiErrorHandlingTests
            {
                protected override void CallAndThrow(Exception e)
                {
                    LoginManager.Login(Arg.Any<Email>(), Arg.Any<string>())
                        .Returns(_ => Observable.Throw<ITogglDataSource>(e));

                    ViewModel.Prepare(LoginType.Login);
                    ViewModel.Email = ValidEmail;
                    ViewModel.NextCommand.Execute();
                    ViewModel.Password = ValidPassword;
                    ViewModel.NextCommand.Execute();
                }
            }

            public sealed class SignUp : BaseApiErrorHandlingTests
            {
                protected override void CallAndThrow(Exception e)
                {
                    LoginManager.SignUp(Arg.Any<Email>(), Arg.Any<string>())
                        .Returns(_ => Observable.Throw<ITogglDataSource>(e));

                    ViewModel.Prepare(LoginType.SignUp);
                    ViewModel.Email = ValidEmail;
                    ViewModel.NextCommand.Execute();
                    ViewModel.Password = ValidPassword;
                    ViewModel.NextCommand.Execute();
                }
            }
        }
    }
}
