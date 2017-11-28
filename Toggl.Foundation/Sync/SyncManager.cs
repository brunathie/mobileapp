using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Ultrawave.Exceptions;
using static Toggl.Foundation.Sync.SyncState;

namespace Toggl.Foundation.Sync
{
    public sealed class SyncManager : ISyncManager
    {
        private readonly object stateLock = new object();
        private readonly ISyncStateQueue queue;
        private readonly IStateMachineOrchestrator orchestrator;

        private bool isFrozen;

        private readonly ISubject<SyncProgress> progress;

        public bool IsRunningSync { get; private set; }

        public SyncState State => orchestrator.State;
        [Obsolete]
        public IObservable<SyncState> StateObservable => orchestrator.StateObservable;
        public IObservable<SyncProgress> ProgressObservable { get; }

        public SyncManager(ISyncStateQueue queue, IStateMachineOrchestrator orchestrator)
        {
            Ensure.Argument.IsNotNull(queue, nameof(queue));
            Ensure.Argument.IsNotNull(orchestrator, nameof(orchestrator));

            this.queue = queue;
            this.orchestrator = orchestrator;

            progress = new BehaviorSubject<SyncProgress>(SyncProgress.Unknown);
            ProgressObservable = progress.AsObservable();

            orchestrator.SyncCompleteObservable.Subscribe(syncOperationCompleted);
            isFrozen = false;
        }

        public IObservable<SyncState> PushSync()
        {
            lock (stateLock)
            {
                queue.QueuePushSync();
                return startSyncIfNeededAndObserve();
            }
        }

        public IObservable<SyncState> ForceFullSync()
        {
            lock (stateLock)
            {
                queue.QueuePullSync();
                return startSyncIfNeededAndObserve();
            }
        }

        public IObservable<SyncState> Freeze()
        {
            lock (stateLock)
            {
                if (isFrozen == false)
                {
                    isFrozen = true;
                    orchestrator.Freeze();
                }

                return IsRunningSync
                    ? syncStatesUntilAndIncludingSleep().LastAsync()
                    : Observable.Return(Sleep);
            }
        }

        private void syncOperationCompleted(SyncResult result)
        {
            lock (stateLock)
            {
                IsRunningSync = false;

                if (result is Success)
                {
                    startSyncIfNeeded();
                    if (IsRunningSync == false)
                    {
                        progress.OnNext(SyncProgress.Synced);
                    }
                    return;
                }

                if (result is Error error)
                {
                    processError(error.Exception);
                    return;
                }

                throw new ArgumentException(nameof(result));
            }
        }

        private void processError(Exception error)
        {
            queue.Clear();
            orchestrator.Start(Sleep);

            if (error is OfflineException)
            {
                progress.OnNext(SyncProgress.OfflineModeDetected);
            }
            else
            {
                progress.OnNext(SyncProgress.Failed);
            }

            if (error is ClientDeprecatedException
                || error is ApiDeprecatedException
                || error is UnauthorizedException)
            {
                Freeze();
                progress.OnError(error);
            }
        }

        private IObservable<SyncState> startSyncIfNeededAndObserve()
        {
            startSyncIfNeeded();
            if (IsRunningSync)
            {
                progress.OnNext(SyncProgress.Syncing);
            }

            return syncStatesUntilAndIncludingSleep();
        }

        private void startSyncIfNeeded()
        {
            if (IsRunningSync) return;

            var state = isFrozen ? Sleep : queue.Dequeue();
            IsRunningSync = state != Sleep;

            orchestrator.Start(state);
        }

        private IObservable<SyncState> syncStatesUntilAndIncludingSleep()
            => StateObservable.TakeWhile(s => s != Sleep)
                .Concat(Observable.Return(Sleep))
                .ConnectedReplay();
    }
}
