using Toggl.Ultrawave.ApiClients;

namespace Toggl.Ultrawave
{
    public interface ITogglApi
    {
        ITagsApi Tags { get; }
        IUserApi User { get; }
        ITasksApi Tasks { get; }
        IStatusApi Status { get; }
        IClientsApi Clients { get; }
        IProjectsApi Projects { get; }
        IWorkspacesApi Workspaces { get; }
        ITimeEntriesApi TimeEntries { get; }
        IWorkspaceFeaturesApi WorkspaceFeatures { get; }
        IReportsApi ReportsApi { get; }
    }
}
