using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FluentAssertions;
using FsCheck.Xunit;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Sync;
using Toggl.Foundation.Tests.Sync.States;
using Toggl.Multivac.Extensions;
using Xunit;
using static Toggl.Foundation.Sync.SyncState;

namespace Toggl.Foundation.Tests.Sync
{
    public sealed class TheDeadlocks
    {
        public abstract class BaseDeadlockTests
        {
            protected ISyncManager SyncManager;
            protected TransitionHandlerProvider Transitions;
            protected IScheduler Scheduler;
            protected IStateMachine StateMachine;
            protected ISyncStateQueue Queue;
            protected IStateMachineOrchestrator Orchestrator;
            protected StateMachineEntryPoints EntryPoints;

            protected BaseDeadlockTests(IScheduler scheduler)
            {
                Scheduler = scheduler;
                Reset();
            }

            protected void Reset()
            {
                Queue = new SyncStateQueue();
                Transitions = new TransitionHandlerProvider();
                Scheduler = new TestScheduler();
                StateMachine = new StateMachine(Transitions, Scheduler, Substitute.For<ISubject<Unit>>());
                EntryPoints = new StateMachineEntryPoints();
                Orchestrator = new StateMachineOrchestrator(StateMachine, EntryPoints);
                SyncManager = new SyncManager(Queue, Orchestrator);
            }

            protected StateResult PreparePullTransitions(byte n)
                => PrepareTransitions(EntryPoints.StartPullSync, n);

            protected StateResult PreparePushTransitions(byte n)
                => PrepareTransitions(EntryPoints.StartPushSync, n);

            protected StateResult PrepareTransitions(StateResult entryPoint, byte n)
            {
                var lastResult = entryPoint;
                for (byte i = 0; i < n; i++)
                {
                    var nextResult = new StateResult();
                    Func<IObservable<ITransition>> transition = () => Observable.Return(new Transition(nextResult));

                    Transitions.ConfigureTransition(lastResult, transition);

                    lastResult = nextResult;
                }

                return lastResult;
            }

            protected void PrepareFailingTransition(StateResult lastResult)
            {
                Func<IObservable<ITransition>> failingTransition = () => Observable.Throw<ITransition>(new TestException());
                Transitions.ConfigureTransition(lastResult, failingTransition);
            }
        }

        public sealed class TheSyncManager : BaseDeadlockTests
        {
            public TheSyncManager() : base(Substitute.For<IScheduler>())
            {
            }

            [Fact, LogIfTooSlow]
            public void DoesNotGetStuckInADeadlockWhenThereAreNoTransitionHandlersForFullSync()
            {
                var isLocked = isLockedAfterFullSync();

                isLocked.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void DoesNotGetStuckInADeadlockWhenThereAreNoTransitionHandlersForPushSync()
            {
                var isLocked = isLockedAfterFullSync();

                isLocked.Should().BeFalse();
            }

            [Property]
            public void DoesNotGetStuckInADeadlockWhenThereAreSomeTransitionHandlersForFullSync(byte n)
            {
                Reset();
                PreparePullTransitions(n);
                PreparePushTransitions(n);

                var isLocked = isLockedAfterFullSync();

                isLocked.Should().BeFalse();
            }

            [Property]
            public void DoesNotGetStuckInADeadlockWhenThereAreSomeTransitionHandlersForPushSync(byte n)
            {
                Reset();
                PreparePushTransitions(n);

                var isLocked = isLockedAfterPushSync();

                isLocked.Should().BeFalse();
            }

            [Property]
            public void DoesNotGetStuckInADeadlockWhenSomeTransitionFailsForFullSync(byte n)
            {
                Reset();
                var lastResult = PreparePullTransitions(n);
                PrepareFailingTransition(lastResult);

                var isLocked = isLockedAfterFullSync();

                isLocked.Should().BeFalse();
            }

            [Property]
            public void DoesNotGetStuckInADeadlockWhenSomeTransitionFailsForPushSync(byte n)
            {
                Reset();
                var lastResult = PreparePushTransitions(n);
                PrepareFailingTransition(lastResult);

                var isLocked = isLockedAfterPushSync();

                isLocked.Should().BeFalse();
            }

            private bool isLockedAfterFullSync()
                => isLockedAfterSync(SyncManager.ForceFullSync);

            private bool isLockedAfterPushSync()
                => isLockedAfterSync(SyncManager.PushSync);

            private bool isLockedAfterSync(Func<IObservable<SyncState>> sync)
            {
                var stateObservable = sync();
                stateObservable.SkipWhile(state => state != Sleep).Wait();
                // now that the sync finished the sync manager must have the IsRunningSync
                // set to false, otherwise no further synchronizations are possible and the
                // internal state is inconsistent and can't be fixed
                return SyncManager.IsRunningSync;
            }
        }

        public sealed class TheStateMachine : BaseDeadlockTests
        {
            public TheStateMachine() : base(new TestScheduler())
            {
            }

            [Fact, LogIfTooSlow]
            public void DoesNotGetStuckInADeadlockWhenThereIsNoTransitionHandler()
            {
                var someResult = new StateResult();
                var differentResult = new StateResult();

                var secondStart = startStateMachineAndPrepareSecondStart(someResult, differentResult);

                secondStart.ShouldNotThrow<InvalidOperationException>();
            }

            [Property]
            public void DoesNotGetStuckInADeadlockWhenThereAreSomeTransitionHandlers(byte n)
            {
                Reset();
                var someResult = new StateResult();
                PrepareTransitions(someResult, n);

                var secondStart = startStateMachineAndPrepareSecondStart(someResult, someResult);

                secondStart.ShouldNotThrow<InvalidOperationException>();
            }

            [Property]
            public void DoesNotGetStuckInADeadlockWhenATransitionHandlerFails(byte n)
            {
                Reset();
                var someResult = new StateResult();
                var lastResult = PrepareTransitions(someResult, n);
                PrepareFailingTransition(lastResult);

                var secondStart = startStateMachineAndPrepareSecondStart(someResult, someResult);

                secondStart.ShouldNotThrow<InvalidOperationException>();
            }

            [Property]
            public void DoesNotGetStuckInADeadlockWhenSomeTransitionTimeOuts(byte n)
            {
                Reset();
                var someResult = new StateResult();
                var lastResult = PrepareTransitions(someResult, n);
                Transitions.ConfigureTransition(lastResult, () => Observable.Never<ITransition>());

                var observable = stateMachineFinised();
                StateMachine.Start(someResult.Transition());
                ((TestScheduler)Scheduler).AdvanceBy(TimeSpan.FromSeconds(62).Ticks);
                observable.Wait();
                Action secondStart = () => StateMachine.Start(someResult.Transition());

                secondStart.ShouldNotThrow<InvalidOperationException>();
            }

            private Action startStateMachineAndPrepareSecondStart(StateResult first, StateResult second)
            {
                var observable = stateMachineFinised();
                StateMachine.Start(first.Transition());
                observable.Wait();
                return () => StateMachine.Start(second.Transition());
            }

            private IObservable<StateMachineEvent> stateMachineFinised()
                => StateMachine.StateTransitions
                    .ConnectedReplay()
                    .SkipWhile(isNotFinished)
                    .FirstAsync();

            private bool isNotFinished(StateMachineEvent next)
                => !(next is StateMachineDeadEnd || next is StateMachineError);
        }
    }
}
