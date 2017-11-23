namespace Toggl.Foundation.Sync
{
    public interface ISyncStateQueue
    {
        void QueuePushSync();
        void QueuePullSync();

        SyncState Dequeue();
        void Clear();
    }
}
