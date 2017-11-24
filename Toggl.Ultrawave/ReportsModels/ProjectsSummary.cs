using System;
using System.Collections.Generic;
using Toggl.Multivac.ReportsModels;

namespace Toggl.Ultrawave.ReportsModels
{
    internal sealed class ProjectsSummary : IProjectsSummary
    {
        public DateTimeOffset StartDate { get; set; }

        public DateTimeOffset? EndDate { get; set; }

        public List<IProjectSummary> ProjectsSummaries { get; set; }
    }
}
