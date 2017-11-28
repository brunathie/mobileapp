using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MvvmCross.Core.Navigation;
using MvvmCross.Core.ViewModels;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Toggl.Ultrawave.Exceptions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class MainViewModel : MvxViewModel
    {
        private bool isWelcome = false;
        private CompositeDisposable disposeBag = new CompositeDisposable();

        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IMvxNavigationService navigationService;
        private readonly IAccessRestrictionStorage accessRestrictionStorage;

        public TimeSpan CurrentTimeEntryElapsedTime { get; private set; } = TimeSpan.Zero;

        private DateTimeOffset? currentTimeEntryStart;

        public long? CurrentTimeEntryId { get; private set; }

        public bool HasCurrentTimeEntry => CurrentTimeEntryId != null;

        public string CurrentTimeEntryDescription { get; private set; }

        public string CurrentTimeEntryProject { get; private set; }

        public string CurrentTimeEntryProjectColor { get; private set; }

        public string CurrentTimeEntryTask { get; private set; }

        public string CurrentTimeEntryClient { get; private set; }

        public bool IsSyncing { get; private set; }

        public bool SpiderIsVisible { get; set; } = true;

        public IMvxAsyncCommand StartTimeEntryCommand { get; }

        public IMvxAsyncCommand StopTimeEntryCommand { get; }

        public IMvxAsyncCommand EditTimeEntryCommand { get; }

        public IMvxAsyncCommand OpenSettingsCommand { get; }

        public IMvxCommand RefreshCommand { get; }

        public MainViewModel(
            ITogglDataSource dataSource,
            ITimeService timeService,
            IMvxNavigationService navigationService,
            IAccessRestrictionStorage accessRestrictionStorage)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(accessRestrictionStorage, nameof(accessRestrictionStorage));

            this.dataSource = dataSource;
            this.timeService = timeService;
            this.navigationService = navigationService;
            this.accessRestrictionStorage = accessRestrictionStorage;

            RefreshCommand = new MvxCommand(refresh);
            OpenSettingsCommand = new MvxAsyncCommand(openSettings);
            EditTimeEntryCommand = new MvxAsyncCommand(editTimeEntry);
            StopTimeEntryCommand = new MvxAsyncCommand(stopTimeEntry);
            StartTimeEntryCommand = new MvxAsyncCommand(startTimeEntry);
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            var tickDisposable = timeService
                .CurrentDateTimeObservable
                .Where(_ => currentTimeEntryStart != null)
                .Subscribe(currentTime => CurrentTimeEntryElapsedTime = currentTime - currentTimeEntryStart.Value);

            var currentlyRunningTimeEntryDisposable = dataSource.TimeEntries
                .CurrentlyRunningTimeEntry
                .Subscribe(setRunningEntry);

            var syncManagerDisposable =
                dataSource.SyncManager.ProgressObservable
                    .Subscribe(progress => IsSyncing = progress == SyncProgress.Syncing, onSyncingError);

            var spiderDisposable =
                dataSource.TimeEntries.IsEmpty
                    .Subscribe(isEmpty => SpiderIsVisible = !isWelcome && isEmpty);

            disposeBag.Add(tickDisposable);
            disposeBag.Add(spiderDisposable);
            disposeBag.Add(syncManagerDisposable);
            disposeBag.Add(currentlyRunningTimeEntryDisposable);
        }

        public override void ViewAppeared()
        {
            base.ViewAppeared();
            navigationService.Navigate<SuggestionsViewModel>();
            navigationService.Navigate<TimeEntriesLogViewModel>();
        }

        private void setRunningEntry(IDatabaseTimeEntry timeEntry)
        {
            CurrentTimeEntryId = timeEntry?.Id;
            currentTimeEntryStart = timeEntry?.Start;
            CurrentTimeEntryDescription = timeEntry?.Description ?? "";

            CurrentTimeEntryTask = timeEntry?.Task?.Name ?? "";
            CurrentTimeEntryProject = timeEntry?.Project?.Name ?? "";
            CurrentTimeEntryProjectColor = timeEntry?.Project?.Color ?? "";
            CurrentTimeEntryClient = timeEntry?.Project?.Client?.Name ?? "";
        }

        private void refresh()
        {
            dataSource.SyncManager.ForceFullSync();
        }

        private Task openSettings()
            => navigationService.Navigate<SettingsViewModel>();

        private Task startTimeEntry() =>
            navigationService.Navigate<StartTimeEntryViewModel, DateTimeOffset>(timeService.CurrentDateTime);

        private async Task stopTimeEntry()
        {
            await dataSource.TimeEntries.Stop(timeService.CurrentDateTime)
                .Do(_ => dataSource.SyncManager.PushSync());

            CurrentTimeEntryElapsedTime = TimeSpan.Zero;
        }

        private Task editTimeEntry()
            => navigationService.Navigate<EditTimeEntryViewModel, long>(CurrentTimeEntryId.Value);

        private void onSyncingError(Exception exception)
        {
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
                case UnauthorizedException unauthorized:
                    accessRestrictionStorage.SetUnauthorizedAccess();
                    navigationService.Navigate<TokenResetViewModel>();
                    return;
                default:
                    throw new ArgumentException(nameof(exception));
            }
        }
    }
}
