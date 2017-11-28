using Toggl.Multivac.ReportsModels;

namespace Toggl.Ultrawave.ReportsModels
{
    internal sealed class ProjectSummary : IProjectSummary
    {
        public long UserId { get; set; }

        public long? ProjectId { get; set; }

        public long TrackedSeconds { get; set; }

        public long? BillableSeconds { get; set; }
    }
}
