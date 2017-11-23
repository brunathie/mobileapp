using static Toggl.Foundation.Sync.SyncState;

namespace Toggl.Foundation.Sync
{
    internal sealed class SyncStateQueue : ISyncStateQueue
    {
        private bool pulledLast;
        private bool pullSyncQueued;
        private bool pushSyncQueued;
        
        public void QueuePushSync()
        {
            pushSyncQueued = true;
        }

        public void QueuePullSync()
        {
            pullSyncQueued = true;
        }

        public SyncState Dequeue()
        {
            if (pulledLast)
                return push();

            if (pullSyncQueued)
                return pull();

            if (pushSyncQueued)
                return push();

            return sleep();
        }

        public void Clear()
        {
            pulledLast = false;
            pullSyncQueued = false;
            pushSyncQueued = false;
        }

        private SyncState pull()
        {
            pullSyncQueued = false;
            pulledLast = true;
            return Pull;
        }

        private SyncState push()
        {
            pushSyncQueued = false;
            pulledLast = false;
            return Push;
        }

        private SyncState sleep()
            => Sleep;
    }
}
