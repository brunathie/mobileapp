using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.Sync;
using Toggl.Multivac;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave;

namespace Toggl.Foundation.DataSources
{
    public sealed class TogglDataSource : ITogglDataSource
    {
        private readonly ITogglDatabase database;

        public TogglDataSource(ITogglDatabase database, ITogglApi api, ITimeService timeService, IScheduler scheduler)
        {
            Ensure.Argument.IsNotNull(api, nameof(api));
            Ensure.Argument.IsNotNull(database, nameof(database));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(scheduler, nameof(scheduler));

            this.database = database;

            User = new UserDataSource(database.User);
            Tags = new TagsDataSource(database.IdProvider, database.Tags, timeService);
            Tasks = new TasksDataSource(database.Tasks);
            Workspaces = new WorkspacesDataSource(database);
            Clients = new ClientsDataSource(database.IdProvider, database.Clients, timeService);
            Projects = new ProjectsDataSource(database.IdProvider, database.Projects, timeService);
            TimeEntries = new TimeEntriesDataSource(database.IdProvider, database.TimeEntries, timeService);

            AutocompleteProvider = new AutocompleteProvider(database);
            SyncManager = TogglSyncManager.CreateSyncManager(database, api, this, timeService, scheduler);
        }

        public IUserSource User { get; }
        public ITagsSource Tags { get; }
        public ITasksSource Tasks { get; }
        public IClientsSource Clients { get; }
        public IProjectsSource Projects { get; }
        public IWorkspacesSource Workspaces { get; }
        public ITimeEntriesSource TimeEntries { get; }

        public ISyncManager SyncManager { get; }
        public IAutocompleteProvider AutocompleteProvider { get; }

        public IObservable<bool> HasUnsyncedData()
            => Observable.Merge(
                hasUnsyncedData(database.TimeEntries),
                hasUnsyncedData(database.Projects),
                hasUnsyncedData(database.User),
                hasUnsyncedData(database.Tasks),
                hasUnsyncedData(database.Clients),
                hasUnsyncedData(database.Tags),
                hasUnsyncedData(database.Workspaces))
                .Any(hasUnsynced => hasUnsynced);

        private IObservable<bool> hasUnsyncedData<TModel>(IRepository<TModel> repository)
            where TModel : IDatabaseSyncable
            => repository
                .GetAll(entity => entity.SyncStatus != SyncStatus.InSync)
                .Select(unsynced => unsynced.Any())
                .SingleAsync();

        public IObservable<Unit> Logout() => database.Clear();
    }
}
