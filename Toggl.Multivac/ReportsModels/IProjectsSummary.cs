using System;
using System.Collections.Generic;

namespace Toggl.Multivac.ReportsModels
{
    public interface IProjectsSummary
    {
        DateTimeOffset StartDate { get; }
        DateTimeOffset? EndDate { get; }
        List<IProjectSummary> ProjectsSummaries { get; }
    }
}
