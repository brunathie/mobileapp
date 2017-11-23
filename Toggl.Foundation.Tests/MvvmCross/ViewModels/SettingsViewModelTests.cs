using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using MvvmCross.Core.Navigation;
using NSubstitute;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Tests.Generators;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SettingsViewModelTests
    {
        public abstract class SettingsViewModelTest : BaseViewModelTests<SettingsViewModel>
        {
            protected ISubject<SyncState> StateObservableSubject;

            protected override void AdditionalSetup()
            {
                StateObservableSubject = new Subject<SyncState>();
                var syncManager = Substitute.For<ISyncManager>();
                var observable = StateObservableSubject.AsObservable();
                syncManager.StateObservable.Returns(observable);
                DataSource.SyncManager.Returns(syncManager);
            }

            protected override SettingsViewModel CreateViewModel()
                => new SettingsViewModel(DataSource, NavigationService, DialogService);
        }

        public sealed class TheConstructor : SettingsViewModelTest
        {
            [Theory]
            [ClassData(typeof(ThreeParameterConstructorTestData))]
            public void ThrowsIfAnyOfTheArgumentsIsNull(bool useDataSource, bool useNavigationService, bool useDialogService)
            {
                var dataSource = useDataSource ? DataSource : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var dialogService = useDialogService ? DialogService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new SettingsViewModel(dataSource, navigationService, dialogService);

                tryingToConstructWithEmptyParameters
                    .ShouldThrow<ArgumentNullException>();
            }
        }

        public sealed class TheFlags : SettingsViewModelTest
        {
            [Property]
            public void SetsIsRunningSyncCorrectly(NonEmptyArray<SyncState> statuses)
            {
                foreach (var state in statuses.Get)
                {
                    StateObservableSubject.OnNext(state);
                    ViewModel.IsRunningSync.Should().Be(state != SyncState.Sleep);
                }
            }

            [Property]
            public void SetsIsSyncedCorrectly(NonEmptyArray<SyncState> statuses)
            {
                foreach (var state in statuses.Get)
                {
                    StateObservableSubject.OnNext(state);
                    ViewModel.IsSynced.Should().Be(state == SyncState.Sleep);
                }
            }

            [Property]
            public void SetsTheIsRunningSyncAndIsSyncedFlagsToOppositeValues(NonEmptyArray<SyncState> statuses)
            {
                foreach (var state in statuses.Get)
                {
                    StateObservableSubject.OnNext(state);
                    ViewModel.IsRunningSync.Should().Be(!ViewModel.IsSynced);
                }
            }

            [Property]
            public void DoesNotSetTheIsLoggingOutFlagIfTheLogoutCommandIsNotExecuted(NonEmptyArray<SyncState> statuses)
            {
                foreach (var state in statuses.Get)
                {
                    StateObservableSubject.OnNext(state);
                    ViewModel.IsLoggingOut.Should().BeFalse();
                }
            }

            [Property]
            public void DoesNotUnsetTheIsLoggingOutFlagAfterItIsSetNoMatterWhatStatusesAreObserved(NonEmptyArray<SyncState> statuses)
            {
                DataSource.SyncManager.Freeze().Returns(Observable.Never<SyncState>());

                ViewModel.LogoutCommand.ExecuteAsync();

                foreach (var state in statuses.Get)
                {
                    StateObservableSubject.OnNext(state);
                    ViewModel.IsLoggingOut.Should().BeTrue();
                }
            }

            [Property]
            public void SetsTheIsRunningSyncAndIsSyncedFlagsToFalseAfterTheIsLoggingInFlagIsSetAndDoesNotSetThemToTrueNoMatterWhatStatusesAreObserved(NonEmptyArray<SyncState> statuses)
            {
                DataSource.SyncManager.Freeze().Returns(Observable.Never<SyncState>());

                ViewModel.LogoutCommand.ExecuteAsync();

                foreach (var state in statuses.Get)
                {
                    StateObservableSubject.OnNext(state);
                    ViewModel.IsRunningSync.Should().BeFalse();
                    ViewModel.IsSynced.Should().BeFalse();
                }
            }
        }

        public sealed class TheLogoutCommand : SettingsViewModelTest
        {
            [Fact]
            public async Task SetsTheIsLoggingOutFlagToTrue()
            {
                doNotShowConfirmationDialog();
                await ViewModel.LogoutCommand.ExecuteAsync();

                ViewModel.IsLoggingOut.Should().BeTrue();
            }

            [Fact]
            public async Task CallsFreezeOnTheSyncManager()
            {
                doNotShowConfirmationDialog();
                await ViewModel.LogoutCommand.ExecuteAsync();

                await DataSource.SyncManager.Received().Freeze();
            }

            [Fact]
            public async Task CallsLogoutOnTheDataSource()
            {
                doNotShowConfirmationDialog();
                await ViewModel.LogoutCommand.ExecuteAsync();

                await DataSource.Received().Logout();
            }

            [Fact]
            public async Task NavigatesToTheOnboardingScreen()
            {
                doNotShowConfirmationDialog();
                await ViewModel.LogoutCommand.ExecuteAsync();

                await NavigationService.Received().Navigate(typeof(OnboardingViewModel));
            }

            [Fact]
            public async Task DoesOperationsInTheCorrectOrder()
            {
                doNotShowConfirmationDialog();
                int operationCounter = 0;
                int flagsAreSet = -1;
                int callingSyncManagerFreeze = -1;
                int awaitingSyncManagerFreeze = -1;
                int callingLogout = -1;
                int awaitingLogout = -1;
                int callingNavigate = -1;
                int awaitingNavigation = -1;
                var syncManager = Substitute.For<ISyncManager>();
                syncManager.Freeze().Returns(Observable.Create<SyncState>(async observer =>
                {
                    if (ViewModel.IsLoggingOut && ViewModel.IsSynced == false && ViewModel.IsRunningSync == false)
                        flagsAreSet = operationCounter++;

                    callingSyncManagerFreeze = operationCounter++;
                    await Task.Delay(100);
                    observer.OnNext(SyncState.Sleep);
                    observer.OnCompleted();
                    awaitingSyncManagerFreeze = operationCounter++;
                    return () => { };
                }));
                DataSource.SyncManager.Returns(syncManager);
                DataSource.Logout().Returns(Observable.Create<Unit>(async observer =>
                {
                    callingLogout = operationCounter++;
                    await Task.Delay(100);
                    observer.OnNext(Unit.Default);
                    observer.OnCompleted();
                    awaitingLogout = operationCounter++;
                    return () => { };
                }));
                NavigationService.Navigate<OnboardingViewModel>().Returns(_ => Task.Run(async () =>
                {
                    callingNavigate = operationCounter++;
                    await Task.Delay(100);
                    awaitingNavigation = operationCounter++;
                }));

                await ViewModel.LogoutCommand.ExecuteAsync();

                flagsAreSet.Should().Be(0);
                callingSyncManagerFreeze.Should().Be(1);
                awaitingSyncManagerFreeze.Should().Be(2);
                callingLogout.Should().Be(3);
                awaitingLogout.Should().Be(4);
                callingNavigate.Should().Be(5);
                awaitingNavigation.Should().Be(6);
            }

            [Fact]
            public void ChecksIfThereAreUnsyncedDataWhenTheSyncProcessFinishes()
            {
                StateObservableSubject.OnNext(SyncState.Sleep);

                DataSource.Received().HasUnsyncedData();
            }

            [Fact]
            public void SetsTheIsSyncedFlagAfterTheSyncProcessHasFinishedAndThereIsNoTimeEntryToPush()
            {
                DataSource.HasUnsyncedData().Returns(Observable.Return(false));
                StateObservableSubject.OnNext(SyncState.Sleep);

                ViewModel.IsSynced.Should().BeTrue();
            }

            [Fact]
            public void UnsetsTheIsSyncedFlagWhenTheSyncProcessIsNotRunningButThrereIsSomeTimeEntryToPush()
            {
                DataSource.HasUnsyncedData().Returns(Observable.Return(true));
                StateObservableSubject.OnNext(SyncState.Sleep);

                ViewModel.IsSynced.Should().BeFalse();
            }

            [Theory]
            [InlineData(SyncState.Pull)]
            [InlineData(SyncState.Push)]
            public void UnsetsTheIsSyncedFlagWhenThereIsNothingToPushButTheSyncProcessStartsAgain(SyncState state)
            {
                DataSource.HasUnsyncedData().Returns(Observable.Return(false));
                StateObservableSubject.OnNext(SyncState.Sleep);
                StateObservableSubject.OnNext(state);

                ViewModel.IsSynced.Should().BeFalse();
            }

            [Fact]
            public async Task DoesNotShowConfirmationDialogWhenTheAppIsInSync()
            {
                doNotShowConfirmationDialog();

                await ViewModel.LogoutCommand.ExecuteAsync();

                await DialogService.DidNotReceiveWithAnyArgs().Confirm("", "", "", "");
            }

            [Fact]
            public async Task ShowsConfirmationDialogWhenThereIsNothingToPushButSyncIsRunning()
            {
                DataSource.HasUnsyncedData().Returns(Observable.Return(false));
                StateObservableSubject.OnNext(SyncState.Pull);

                await ViewModel.LogoutCommand.ExecuteAsync();

                await DialogService.ReceivedWithAnyArgs().Confirm("", "", "", "");
            }

            [Fact]
            public async Task ShowsConfirmationDialogWhenThereIsSomethingToPushButSyncIsNotRunning()
            {
                DataSource.HasUnsyncedData().Returns(Observable.Return(true));
                StateObservableSubject.OnNext(SyncState.Sleep);

                await ViewModel.LogoutCommand.ExecuteAsync();

                await DialogService.ReceivedWithAnyArgs().Confirm("", "", "", "");
            }

            [Fact]
            public async Task DoesNotProceedWithLogoutWhenUserClicksCancelButtonInTheDialog()
            {
                StateObservableSubject.OnNext(SyncState.Pull);
                DialogService.Confirm(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>()).Returns(false);

                await ViewModel.LogoutCommand.ExecuteAsync();

                ViewModel.IsLoggingOut.Should().BeFalse();
                await DataSource.SyncManager.DidNotReceive().Freeze();
                await DataSource.DidNotReceive().Logout();
                await NavigationService.DidNotReceive().Navigate<OnboardingViewModel>();
            }

            [Fact]
            public async Task ProceedsWithLogoutWhenUserClicksSignOutButtonInTheDialog()
            {
                StateObservableSubject.OnNext(SyncState.Pull);
                DialogService.Confirm(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>()).Returns(true);

                await ViewModel.LogoutCommand.ExecuteAsync();

                ViewModel.IsLoggingOut.Should().BeTrue();
                await DataSource.SyncManager.Received().Freeze();
                await DataSource.Received().Logout();
                await NavigationService.Received().Navigate<OnboardingViewModel>();
            }

            private void doNotShowConfirmationDialog()
            {
                DataSource.HasUnsyncedData().Returns(Observable.Return(false));
                StateObservableSubject.OnNext(SyncState.Sleep);
            }
        }

        public sealed class TheBackCommand : SettingsViewModelTest
        {
            [Fact]
            public async Task ClosesTheViewModel()
            {
                await ViewModel.BackCommand.ExecuteAsync();

                await NavigationService.Received().Close(ViewModel);
            }
        }

        public sealed class TheToggleAddMobileTagCommand : SettingsViewModelTest
        {
            [Fact]
            public void TogglesTheCurrentValueOfTheToggleAddMobileTagProperty()
            {
                var expected = !ViewModel.AddMobileTag;

                ViewModel.ToggleAddMobileTagCommand.Execute();

                ViewModel.AddMobileTag.Should().Be(expected);
            }
        }

        public sealed class TheToggleUseTwentyFourHourClockCommand : SettingsViewModelTest
        {
            [Fact]
            public void TogglesTheCurrentValueOfTheToggleUseTwentyFourHourClockProperty()
            {
                var expected = !ViewModel.UseTwentyFourHourClock;

                ViewModel.ToggleUseTwentyFourHourClockCommand.Execute();

                ViewModel.UseTwentyFourHourClock.Should().Be(expected);
            }
        }
    }
}
