namespace Toggl.Foundation.MvvmCross.Parameters
{
    public sealed class WorkspaceParameters
    {
        public string Title { get; set; }

        public bool AllowQuerying { get; set; }

        public long CurrentWorkspaceId { get; set; }

        public static WorkspaceParameters Create(long currentWorkspaceId, string title, bool allowQuerying) => new WorkspaceParameters
        {
            Title = title,
            AllowQuerying = allowQuerying,
            CurrentWorkspaceId = currentWorkspaceId
        };
    }
}
