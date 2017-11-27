using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck.Xunit;
using MvvmCross.Core.Navigation;
using NSubstitute;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Tests.Generators;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Network;
using Xunit;
using TimeEntry = Toggl.Foundation.Models.TimeEntry;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class MainViewModelTests
    {
        public abstract class MainViewModelTest : BaseViewModelTests<MainViewModel>
        {
            protected IAccessRestrictionStorage AccessRestrictionStorage { get; } = Substitute.For<IAccessRestrictionStorage>();

            protected ISubject<SyncProgress> ProgressSubject { get; } = new Subject<SyncProgress>();

            protected override MainViewModel CreateViewModel()
                => new MainViewModel(DataSource, TimeService, NavigationService, AccessRestrictionStorage);

            protected override void AdditionalSetup()
            {
                base.AdditionalSetup();

                var syncManager = Substitute.For<ISyncManager>();
                syncManager.ProgressObservable.Returns(ProgressSubject.AsObservable());
                DataSource.SyncManager.Returns(syncManager);
            }
        }

        public sealed class TheConstructor : MainViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ClassData(typeof(FourParameterConstructorTestData))]
            public void ThrowsIfAnyOfTheArgumentsIsNull(bool useDataSource, bool useTimeService, bool useNavigationService, bool useAccessRestrictionStorage)
            {
                var dataSource = useDataSource ? DataSource : null;
                var timeService = useTimeService ? TimeService : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var accessRestrictionStorage = useAccessRestrictionStorage ? AccessRestrictionStorage : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new MainViewModel(dataSource, timeService, navigationService, accessRestrictionStorage);

                tryingToConstructWithEmptyParameters
                    .ShouldThrow<ArgumentNullException>();
            }
        }

        public sealed class TheViewAppearedMethod : MainViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void RequestsTheSuggestionsViewModel()
            {
                ViewModel.ViewAppeared();

                NavigationService.Received().Navigate(typeof(SuggestionsViewModel));
            }

            [Fact, LogIfTooSlow]
            public void RequestsTheLogTimeEntriesViewModel()
            {
                ViewModel.ViewAppeared();

                NavigationService.Received().Navigate(typeof(TimeEntriesLogViewModel));
            }
        }

        public sealed class TheStartTimeEntryCommand : MainViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheStartTimeEntryViewModel()
            {
                await ViewModel.StartTimeEntryCommand.ExecuteAsync();

                await NavigationService.Received().Navigate(typeof(StartTimeEntryViewModel), Arg.Any<DateTimeOffset>());
            }

            [Property]
            public void PassesTheCurrentDateToTheStartTimeEntryViewModel(DateTimeOffset date)
            {
                TimeService.CurrentDateTime.Returns(date);

                ViewModel.StartTimeEntryCommand.ExecuteAsync().Wait();

                NavigationService.Received().Navigate(
                    typeof(StartTimeEntryViewModel),
                    Arg.Is<DateTimeOffset>(parameter => parameter == date)
                ).Wait();
            }
        }

        public sealed class TheOpenSettingsCommand : MainViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheSettingsViewModel()
            {
                await ViewModel.OpenSettingsCommand.ExecuteAsync();

                await NavigationService.Received().Navigate(typeof(SettingsViewModel));
            }
        }

        public sealed class TheStopTimeEntryCommand : MainViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task CallsTheStopMethodOnTheDataSource()
            {
                var date = DateTimeOffset.UtcNow;
                TimeService.CurrentDateTime.Returns(date);

                await ViewModel.StopTimeEntryCommand.ExecuteAsync();

                await DataSource.TimeEntries.Received().Stop(date);
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheElapsedTimeToZero()
            {
                await ViewModel.StopTimeEntryCommand.ExecuteAsync();

                ViewModel.CurrentTimeEntryElapsedTime.Should().Be(TimeSpan.Zero);
            }

            [Fact, LogIfTooSlow]
            public async Task InitiatesPushSync()
            {
                ViewModel.StopTimeEntryCommand.Execute();

                await DataSource.SyncManager.Received().PushSync();
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotInitiatePushSyncWhenSavingFails()
            {
                DataSource.TimeEntries.Stop(Arg.Any<DateTimeOffset>())
                    .Returns(Observable.Throw<IDatabaseTimeEntry>(new Exception()));

                ViewModel.StopTimeEntryCommand.Execute();

                await DataSource.SyncManager.DidNotReceive().PushSync();
            }
        }

        public abstract class CurrentTimeEntrypropertyTest<T> : MainViewModelTest
        {
            private BehaviorSubject<IDatabaseTimeEntry> currentTimeEntrySubject
                = new BehaviorSubject<IDatabaseTimeEntry>(null);

            protected abstract T ActualValue { get; }
            protected abstract T ExpectedValue { get; }
            protected abstract T ExpectedEmptyValue { get; }

            protected long TimeEntryId = 13;
            protected string Description = "Something";
            protected string Project = "Some project";
            protected string Task = "Some task";
            protected string Client = "Some client";
            protected string ProjectColor = "0000AF";

            private async Task prepare()
            {
                var timeEntry = Substitute.For<IDatabaseTimeEntry>();
                timeEntry.Id.Returns(TimeEntryId);
                timeEntry.Description.Returns(Description);
                timeEntry.Project.Name.Returns(Project);
                timeEntry.Project.Color.Returns(ProjectColor);
                timeEntry.Task.Name.Returns(Task);
                timeEntry.Project.Client.Name.Returns(Client);

                DataSource.TimeEntries.CurrentlyRunningTimeEntry.Returns(currentTimeEntrySubject.AsObservable());

                await ViewModel.Initialize();
                currentTimeEntrySubject.OnNext(timeEntry);
            }

            [Fact, LogIfTooSlow]
            public async Task IsSet()
            {
                await prepare();

                ActualValue.Should().Be(ExpectedValue);
            }

            [Fact, LogIfTooSlow]
            public async Task IsUnset()
            {
                await prepare();
                currentTimeEntrySubject.OnNext(null);

                ActualValue.Should().Be(ExpectedEmptyValue);
            }
        }

        public sealed class TheCurrentTimeEntryIdProperty : CurrentTimeEntrypropertyTest<long?>
        {
            protected override long? ActualValue => ViewModel.CurrentTimeEntryId;

            protected override long? ExpectedValue => TimeEntryId;

            protected override long? ExpectedEmptyValue => null;
        }

        public sealed class TheCurrentTimeEntryDescriptionProperty : CurrentTimeEntrypropertyTest<string>
        {
            protected override string ActualValue => ViewModel.CurrentTimeEntryDescription;

            protected override string ExpectedValue => Description;

            protected override string ExpectedEmptyValue => "";
        }

        public sealed class TheCurrentTimeEntryProjectProperty : CurrentTimeEntrypropertyTest<string>
        {
            protected override string ActualValue => ViewModel.CurrentTimeEntryProject;

            protected override string ExpectedValue => Project;

            protected override string ExpectedEmptyValue => "";
        }

        public sealed class TheCurrentTimeEntryProjectColorProperty : CurrentTimeEntrypropertyTest<string>
        {
            protected override string ActualValue => ViewModel.CurrentTimeEntryProjectColor;

            protected override string ExpectedValue => ProjectColor;

            protected override string ExpectedEmptyValue => "";
        }

        public sealed class TheCurrentTimeEntryTaskProperty : CurrentTimeEntrypropertyTest<string>
        {
            protected override string ActualValue => ViewModel.CurrentTimeEntryTask;

            protected override string ExpectedValue => Task;

            protected override string ExpectedEmptyValue => "";
        }

        public sealed class TheCurrentTimeEntryClientProperty : CurrentTimeEntrypropertyTest<string>
        {
            protected override string ActualValue => ViewModel.CurrentTimeEntryClient;

            protected override string ExpectedValue => Client;

            protected override string ExpectedEmptyValue => "";
        }

        public sealed class TheHasCurrentTimeEntryProperty : CurrentTimeEntrypropertyTest<bool>
        {
            protected override bool ActualValue => ViewModel.HasCurrentTimeEntry;

            protected override bool ExpectedValue => true;

            protected override bool ExpectedEmptyValue => false;
        }

        public sealed class SyncErrorHandling : MainViewModelTest
        {
            private IRequest request => Substitute.For<IRequest>();
            private IResponse response => Substitute.For<IResponse>();

            [Fact, LogIfTooSlow]
            public async Task SetsTheOutdatedClientVersionFlag()
            {
                await ViewModel.Initialize();

                ProgressSubject.OnError(new ClientDeprecatedException(request, response));

                AccessRestrictionStorage.Received().SetClientOutdated();
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheOutdatedApiVersionFlag()
            {
                await ViewModel.Initialize();

                ProgressSubject.OnError(new ApiDeprecatedException(request, response));

                AccessRestrictionStorage.Received().SetApiOutdated();
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheUnauthorizedAccessFlag()
            {
                await ViewModel.Initialize();

                ProgressSubject.OnError(new UnauthorizedException(request, response));

                AccessRestrictionStorage.Received().SetUnauthorizedAccess();
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheOutdatedClientScreen()
            {
                await ViewModel.Initialize();

                ProgressSubject.OnError(new ClientDeprecatedException(request, response));

                await NavigationService.Received().Navigate<OutdatedAppViewModel>();
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheOutdatedApiScreen()
            {
                await ViewModel.Initialize();

                ProgressSubject.OnError(new ApiDeprecatedException(request, response));

                await NavigationService.Received().Navigate<OutdatedAppViewModel>();
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheRepeatLoginScreen()
            {
                await ViewModel.Initialize();

                ProgressSubject.OnError(new UnauthorizedException(request, response));

                await NavigationService.Received().Navigate<TokenResetViewModel>();
            }
        }
    }
}
