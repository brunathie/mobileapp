using Toggl.Ultrawave.ReportsApiClients;

namespace Toggl.Ultrawave
{
    public interface IReportsApi
    {
        IProjectsSummaryApi ProjectsSummary { get; }
    }
}
