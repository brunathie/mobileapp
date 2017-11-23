using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Toggl.Foundation.Sync;
using static Toggl.Foundation.Sync.SyncState;
using Xunit;

namespace Toggl.Foundation.Tests.Sync
{
    public sealed class SyncStateQueueTests
    {
        public abstract class BaseStateQueueTests
        {
            private readonly SyncStateQueue queue = new SyncStateQueue();

            protected List<SyncState> DequeuedStates { get; } = new List<SyncState>();

            protected void QueueSyncs(params SyncState[] states)
            {
                foreach (var state in states)
                    switch (state)
                    {
                        case Pull:
                            queue.QueuePullSync();
                            break;
                        case Push:
                            queue.QueuePushSync();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
            }

            protected SyncState Dequeue()
            {
                var state = queue.Dequeue();
                DequeuedStates.Add(state);
                return state;
            }

            protected void Clear() => queue.Clear();

            protected List<SyncState> DequeueUntilSleep()
            {
                var returnValues = new List<SyncState>();

                while (true)
                {
                    returnValues.Add(Dequeue());

                    if (DequeuedStates.Last() == Sleep)
                        break;
                }

                return returnValues;
            }

            protected SyncState[] BoolToEnumValues(bool[] pushPull)
                => (pushPull ?? Enumerable.Empty<bool>())
                    .Select(b => b ? Push : Pull).ToArray();
        }

        public sealed class TheStartNextQueuedStateMethod : BaseStateQueueTests
        {
            [Fact]
            public void StartsSleepIfNothingQueued()
            {
                Dequeue();

                DequeuedStates.ShouldBeSameEventsAs(
                    Sleep
                );
            }

            [Fact]
            public void StartsPullIfOnlyPullQueued()
            {
                QueueSyncs(Pull);

                Dequeue();

                DequeuedStates.ShouldBeSameEventsAs(
                    Pull
                );
            }

            [Fact]
            public void StartsPushIfOnlyPushQueued()
            {
                QueueSyncs(Push);

                Dequeue();

                DequeuedStates.ShouldBeSameEventsAs(
                    Push
                );
            }

            [Property]
            public void AlwaysReturnsTheStateItRuns(bool[] pushPull)
            {
                DequeuedStates.Clear();
                QueueSyncs(BoolToEnumValues(pushPull));

                var returnValues = DequeueUntilSleep();

                returnValues.ShouldBeSameEventsAs(DequeuedStates.ToArray());
            }

            [Property]
            public void RunsFullCycleOnceNoMatterWhatIsQueuedIfPullIsQueued(bool[] pushPull)
            {
                DequeuedStates.Clear();
                var states = (pushPull ?? Enumerable.Empty<bool>())
                    .Select(b => b ? Push : Pull).ToArray();
                QueueSyncs(states);
                if (!states.Contains(Pull))
                    QueueSyncs(Pull);

                DequeueUntilSleep();

                DequeuedStates.ShouldBeSameEventsAs(
                    Pull, Push, Sleep
                );
            }

            [Fact]
            public void RunsFullCycleIfPullIsQueuedAfterPushStarted()
            {
                QueueSyncs(Push);
                Dequeue();
                QueueSyncs(Pull);

                DequeueUntilSleep();

                DequeuedStates.ShouldBeSameEventsAs(
                    Push, Pull, Push, Sleep
                );
            }

            [Fact]
            public void RunsTwoFullCyclesIfPullIsQueuedAfterFirstPullStarted()
            {
                QueueSyncs(Pull);
                Dequeue();
                QueueSyncs(Pull);

                DequeueUntilSleep();

                DequeuedStates.ShouldBeSameEventsAs(
                    Pull, Push, Pull, Push, Sleep
                );
            }
        }

        public sealed class TheClearMethod : BaseStateQueueTests
        {
            [Fact]
            public void ClearsPush()
            {
                QueueSyncs(Push);

                Clear();
                var returnValues = DequeueUntilSleep();

                returnValues.ShouldBeSameEventsAs(Sleep);
            }

            [Fact]
            public void ClearsPull()
            {
                QueueSyncs(Pull);

                Clear();
                var returnValues = DequeueUntilSleep();

                returnValues.ShouldBeSameEventsAs(Sleep);
            }

            [Property]
            public void RetursSleepAfterClearNoMatterWhatWasQueuedPreviously(NonEmptyArray<bool> pushPull)
            {
                DequeuedStates.Clear();
                QueueSyncs(BoolToEnumValues(pushPull.Get));

                Clear();
                var returnValues = DequeueUntilSleep();

                returnValues.ShouldBeSameEventsAs(Sleep);
            }
        }
    }
}
