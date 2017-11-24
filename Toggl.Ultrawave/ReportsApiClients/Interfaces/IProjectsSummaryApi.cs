using System;
using Toggl.Multivac.ReportsModels;

namespace Toggl.Ultrawave.ReportsApiClients
{
    public interface IProjectsSummaryApi
    {
        IObservable<IProjectsSummary> GetByWorkspace(long workspaceId, DateTimeOffset startDate, DateTimeOffset? endDate);
    }
}
