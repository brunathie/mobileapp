using System;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Sync.States;
using Toggl.PrimeRadiant;
using Toggl.Ultrawave;
using System.Reactive.Concurrency;
using Toggl.Foundation.Tests.Sync.States;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant.Models;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Sync.States.Push;
using System.Reactive.Linq;
using System.Reactive;
using System.Reactive.Subjects;

namespace Toggl.Foundation
{
    public static class TogglSyncManager
    {
        public static ISyncManager CreateSyncManager(
            ITogglDatabase database,
            ITogglApi api,
            ITogglDataSource dataSource,
            ITimeService timeService,
            IScheduler scheduler)
        {
            var queue = new SyncStateQueue();
            var entryPoints = new StateMachineEntryPoints();
            var transitions = new TransitionHandlerProvider();
            var delayCancellation = new Subject<Unit>();
            var delayCancellationObservable = delayCancellation.AsObservable().Replay();
            ConfigureTransitions(transitions, database, api, dataSource, scheduler, timeService, entryPoints, delayCancellationObservable);
            var stateMachine = new StateMachine(transitions, scheduler, delayCancellation);
            var orchestrator = new StateMachineOrchestrator(stateMachine, entryPoints);

            return new SyncManager(queue, orchestrator);
        }

        public static void ConfigureTransitions(
            TransitionHandlerProvider transitions,
            ITogglDatabase database,
            ITogglApi api,
            ITogglDataSource dataSource,
            IScheduler scheduler,
            ITimeService timeService,
            StateMachineEntryPoints entryPoints,
            IObservable<Unit> delayCancellation)
        {
            configurePullTransitions(transitions, database, api, dataSource, timeService, entryPoints.StartPullSync);
            configurePushTransitions(transitions, database, api, dataSource, scheduler, entryPoints.StartPushSync, delayCancellation);
        }

        private static void configurePullTransitions(
            TransitionHandlerProvider transitions,
            ITogglDatabase database,
            ITogglApi api,
            ITogglDataSource dataSource,
            ITimeService timeService,
            StateResult entryPoint)
        {
            var fetchAllSince = new FetchAllSinceState(database, api, timeService);
            var persistWorkspaces = new PersistWorkspacesState(database.Workspaces, database.SinceParameters);
            var persistWorkspaceFeatures = new PersistWorkspacesFeaturesState(database.WorkspaceFeatures, database.SinceParameters);
            var persistTags = new PersistTagsState(database.Tags, database.SinceParameters);
            var persistClients = new PersistClientsState(database.Clients, database.SinceParameters);
            var persistProjects = new PersistProjectsState(database.Projects, database.SinceParameters);
            var persistTimeEntries = new PersistTimeEntriesState(dataSource.TimeEntries, database.SinceParameters, timeService);
            var persistTasks = new PersistTasksState(database.Tasks, database.SinceParameters);

            transitions.ConfigureTransition(entryPoint, fetchAllSince.Start);
            transitions.ConfigureTransition(fetchAllSince.FetchStarted, persistWorkspaces.Start);
            transitions.ConfigureTransition(persistWorkspaces.FinishedPersisting, persistWorkspaceFeatures.Start);
            transitions.ConfigureTransition(persistWorkspaceFeatures.FinishedPersisting, persistTags.Start);
            transitions.ConfigureTransition(persistTags.FinishedPersisting, persistClients.Start);
            transitions.ConfigureTransition(persistClients.FinishedPersisting, persistProjects.Start);
            transitions.ConfigureTransition(persistProjects.FinishedPersisting, persistTasks.Start);
            transitions.ConfigureTransition(persistTasks.FinishedPersisting, persistTimeEntries.Start);
        }
        
        private static void configurePushTransitions(
            TransitionHandlerProvider transitions,
            ITogglDatabase database,
            ITogglApi api,
            ITogglDataSource dataSource,
            IScheduler scheduler,
            StateResult entryPoint,
            IObservable<Unit> delayCancellation)
        {
            var pushingUsersFinished = configurePushTransitionsForUsers(transitions, database, api, scheduler, entryPoint, delayCancellation);
            var pushingTagsFinished = configurePushTransitionsForTags(transitions, database, api, scheduler, pushingUsersFinished, delayCancellation);
            var pushingClientsFinished = configurePushTransitionsForClients(transitions, database, api, scheduler, pushingTagsFinished, delayCancellation);
            var pushingProjectsFinished = configurePushTransitionsForProjects(transitions, database, api, scheduler, pushingClientsFinished, delayCancellation);
            configurePushTransitionsForTimeEntries(transitions, database, api, dataSource, scheduler, pushingProjectsFinished, delayCancellation);
        }

        private static IStateResult configurePushTransitionsForTimeEntries(
            TransitionHandlerProvider transitions,
            ITogglDatabase database,
            ITogglApi api,
            ITogglDataSource dataSource,
            IScheduler scheduler,
            IStateResult entryPoint,
            IObservable<Unit> delayCancellation)
        {
            var rnd = new Random();
            var apiDelay = new RetryDelayService(rnd);
            var statusDelay = new RetryDelayService(rnd);

            var push = new PushTimeEntriesState(database.TimeEntries);
            var pushOne = new PushOneEntityState<IDatabaseTimeEntry>();
            var create = new CreateTimeEntryState(api, dataSource.TimeEntries);
            var update = new UpdateTimeEntryState(api, dataSource.TimeEntries);
            var delete = new DeleteTimeEntryState(api, database.TimeEntries);
            var deleteLocal = new DeleteLocalTimeEntryState(database.TimeEntries);
            var unsyncable = new UnsyncableTimeEntryState(dataSource.TimeEntries);
            var checkServerStatus = new CheckServerStatusState(api, scheduler, apiDelay, statusDelay, delayCancellation);
            var finished = new ResetAPIDelayState(apiDelay);

            return configurePush(transitions, entryPoint, push, pushOne, create, update, delete, deleteLocal, unsyncable, checkServerStatus, finished);
        }

        private static IStateResult configurePushTransitionsForTags(
            TransitionHandlerProvider transitions,
            ITogglDatabase database,
            ITogglApi api,
            IScheduler scheduler,
            IStateResult entryPoint,
            IObservable<Unit> delayCancellation)
        {
            var rnd = new Random();
            var apiDelay = new RetryDelayService(rnd);
            var statusDelay = new RetryDelayService(rnd);

            var push = new PushTagsState(database.Tags);
            var pushOne = new PushOneEntityState<IDatabaseTag>();
            var create = new CreateTagState(api, database.Tags);
            var unsyncable = new UnsyncableTagState(database.Tags);
            var checkServerStatus = new CheckServerStatusState(api, scheduler, apiDelay, statusDelay, delayCancellation);
            var finished = new ResetAPIDelayState(apiDelay);

            return configureCreateOnlyPush(transitions, entryPoint, push, pushOne, create, unsyncable, checkServerStatus, finished);
        }

        private static IStateResult configurePushTransitionsForClients(
            TransitionHandlerProvider transitions,
            ITogglDatabase database,
            ITogglApi api,
            IScheduler scheduler,
            IStateResult entryPoint,
            IObservable<Unit> delayCancellation)
        {
            var rnd = new Random();
            var apiDelay = new RetryDelayService(rnd);
            var statusDelay = new RetryDelayService(rnd);

            var push = new PushClientsState(database.Clients);
            var pushOne = new PushOneEntityState<IDatabaseClient>();
            var create = new CreateClientState(api, database.Clients);
            var unsyncable = new UnsyncableClientState(database.Clients);
            var checkServerStatus = new CheckServerStatusState(api, scheduler, apiDelay, statusDelay, delayCancellation);
            var finished = new ResetAPIDelayState(apiDelay);

            return configureCreateOnlyPush(transitions, entryPoint, push, pushOne, create, unsyncable, checkServerStatus, finished);
        }

        private static IStateResult configurePushTransitionsForProjects(
            TransitionHandlerProvider transitions,
            ITogglDatabase database,
            ITogglApi api,
            IScheduler scheduler,
            IStateResult entryPoint,
            IObservable<Unit> delayCancellation)
        {
            var rnd = new Random();
            var apiDelay = new RetryDelayService(rnd);
            var statusDelay = new RetryDelayService(rnd);

            var push = new PushProjectsState(database.Projects);
            var pushOne = new PushOneEntityState<IDatabaseProject>();
            var create = new CreateProjectState(api, database.Projects);
            var unsyncable = new UnsyncableProjectState(database.Projects);
            var checkServerStatus = new CheckServerStatusState(api, scheduler, apiDelay, statusDelay, delayCancellation);
            var finished = new ResetAPIDelayState(apiDelay);

            return configureCreateOnlyPush(transitions, entryPoint, push, pushOne, create, unsyncable, checkServerStatus, finished);
        }

        private static IStateResult configurePushTransitionsForUsers(
            TransitionHandlerProvider transitions,
            ITogglDatabase database,
            ITogglApi api,
            IScheduler scheduler,
            IStateResult entryPoint,
            IObservable<Unit> delayCancellation)
        {
            var rnd = new Random();
            var apiDelay = new RetryDelayService(rnd);
            var statusDelay = new RetryDelayService(rnd);

            var push = new PushUsersState(database.User);
            var pushOne = new PushOneEntityState<IDatabaseUser>();
            var update = new UpdateUserState(api, database.User);
            var unsyncable = new UnsyncableUserState(database.User);
            var checkServerStatus = new CheckServerStatusState(api, scheduler, apiDelay, statusDelay, delayCancellation);
            var finished = new ResetAPIDelayState(apiDelay);

            return configureUpdateOnlyPush(transitions, entryPoint, push, pushOne, update, unsyncable, checkServerStatus, finished);
        }

        private static IStateResult configurePush<T>(
            TransitionHandlerProvider transitions,
            IStateResult entryPoint,
            BasePushState<T> push,
            PushOneEntityState<T> pushOne,
            BaseCreateEntityState<T> create,
            BaseUpdateEntityState<T> update,
            BaseDeleteEntityState<T> delete,
            BaseDeleteLocalEntityState<T> deleteLocal,
            BaseUnsyncableEntityState<T> markUnsyncable,
            CheckServerStatusState checkServerStatus,
            ResetAPIDelayState finished)
            where T : class, IBaseModel, IDatabaseSyncable
        {
            transitions.ConfigureTransition(entryPoint, push.Start);
            transitions.ConfigureTransition(push.PushEntity, pushOne.Start);
            transitions.ConfigureTransition(pushOne.CreateEntity, create.Start);
            transitions.ConfigureTransition(pushOne.UpdateEntity, update.Start);
            transitions.ConfigureTransition(pushOne.DeleteEntity, delete.Start);
            transitions.ConfigureTransition(pushOne.DeleteEntityLocally, deleteLocal.Start);

            transitions.ConfigureTransition(create.ClientError, markUnsyncable.Start);
            transitions.ConfigureTransition(update.ClientError, markUnsyncable.Start);
            transitions.ConfigureTransition(delete.ClientError, markUnsyncable.Start);

            transitions.ConfigureTransition(create.ServerError, checkServerStatus.Start);
            transitions.ConfigureTransition(update.ServerError, checkServerStatus.Start);
            transitions.ConfigureTransition(delete.ServerError, checkServerStatus.Start);

            transitions.ConfigureTransition(create.UnknownError, checkServerStatus.Start);
            transitions.ConfigureTransition(update.UnknownError, checkServerStatus.Start);
            transitions.ConfigureTransition(delete.UnknownError, checkServerStatus.Start);

            transitions.ConfigureTransition(checkServerStatus.Retry, checkServerStatus.Start);
            transitions.ConfigureTransition(checkServerStatus.ServerIsAvailable, push.Start);

            transitions.ConfigureTransition(create.CreatingFinished, finished.Start);
            transitions.ConfigureTransition(update.UpdatingSucceeded, finished.Start);
            transitions.ConfigureTransition(delete.DeletingFinished, finished.Start);
            transitions.ConfigureTransition(deleteLocal.Deleted, finished.Start);
            transitions.ConfigureTransition(deleteLocal.DeletingFailed, finished.Start);

            transitions.ConfigureTransition(finished.PushNext, push.Start);

            return push.NothingToPush;
        }

        private static IStateResult configureCreateOnlyPush<T>(
            TransitionHandlerProvider transitions,
            IStateResult entryPoint,
            BasePushState<T> push,
            PushOneEntityState<T> pushOne,
            BaseCreateEntityState<T> create,
            BaseUnsyncableEntityState<T> markUnsyncable,
            CheckServerStatusState checkServerStatus,
            ResetAPIDelayState finished)
            where T : class, IBaseModel, IDatabaseSyncable
        {
            transitions.ConfigureTransition(entryPoint, push.Start);
            transitions.ConfigureTransition(push.PushEntity, pushOne.Start);
            transitions.ConfigureTransition(pushOne.CreateEntity, create.Start);

            transitions.ConfigureTransition(pushOne.UpdateEntity, new InvalidTransitionState($"Updating is not supported for {typeof(T).Name} during Push sync.").Start);
            transitions.ConfigureTransition(pushOne.DeleteEntity, new InvalidTransitionState($"Deleting is not supported for {typeof(T).Name} during Push sync.").Start);
            transitions.ConfigureTransition(pushOne.DeleteEntityLocally, new InvalidTransitionState($"Deleting locally is not supported for {typeof(T).Name} during Push sync.").Start);

            transitions.ConfigureTransition(create.ClientError, markUnsyncable.Start);
            transitions.ConfigureTransition(create.ServerError, checkServerStatus.Start);
            transitions.ConfigureTransition(create.UnknownError, checkServerStatus.Start);

            transitions.ConfigureTransition(checkServerStatus.Retry, checkServerStatus.Start);
            transitions.ConfigureTransition(checkServerStatus.ServerIsAvailable, push.Start);

            transitions.ConfigureTransition(create.CreatingFinished, finished.Start);

            transitions.ConfigureTransition(finished.PushNext, push.Start);

            return push.NothingToPush;
        }

        private static IStateResult configureUpdateOnlyPush<T>(
            TransitionHandlerProvider transitions,
            IStateResult entryPoint,
            BasePushState<T> push,
            PushOneEntityState<T> pushOne,
            BaseUpdateEntityState<T> update,
            BaseUnsyncableEntityState<T> markUnsyncable,
            CheckServerStatusState checkServerStatus,
            ResetAPIDelayState finished)
            where T : class, IBaseModel, IDatabaseSyncable
        {
            transitions.ConfigureTransition(entryPoint, push.Start);
            transitions.ConfigureTransition(push.PushEntity, pushOne.Start);
            transitions.ConfigureTransition(pushOne.UpdateEntity, update.Start);

            transitions.ConfigureTransition(pushOne.CreateEntity, new InvalidTransitionState($"Creating is not supported for {typeof(T).Name} during Push sync.").Start);
            transitions.ConfigureTransition(pushOne.DeleteEntity, new InvalidTransitionState($"Deleting is not supported for {typeof(T).Name} during Push sync.").Start);
            transitions.ConfigureTransition(pushOne.DeleteEntityLocally, new InvalidTransitionState($"Deleting locally is not supported for {typeof(T).Name} during Push sync.").Start);

            transitions.ConfigureTransition(update.ClientError, markUnsyncable.Start);
            transitions.ConfigureTransition(update.ServerError, checkServerStatus.Start);
            transitions.ConfigureTransition(update.UnknownError, checkServerStatus.Start);

            transitions.ConfigureTransition(checkServerStatus.Retry, checkServerStatus.Start);
            transitions.ConfigureTransition(checkServerStatus.ServerIsAvailable, push.Start);

            transitions.ConfigureTransition(update.UpdatingSucceeded, finished.Start);

            transitions.ConfigureTransition(finished.PushNext, push.Start);

            return push.NothingToPush;
        }
    }
}
