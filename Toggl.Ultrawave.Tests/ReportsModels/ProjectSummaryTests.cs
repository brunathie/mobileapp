using Toggl.Ultrawave.ReportsModels;
using Xunit;

namespace Toggl.Ultrawave.Tests.Models
{
    public sealed class ProjectSummaryTests
    {
        public sealed class TheProjectSummaryModel
        {
            private string validJson
                => "{\"user_id\":23741667,\"project_id\":1427273,\"tracked_seconds\":9876,\"billed_seconds\":6543}";

            private ProjectSummary validSummary => new ProjectSummary
            {
                UserId = 23741667,
                ProjectId = 1427273,
                TrackedSeconds = 9876,
                BilledSeconds = 6543
            };

            [Fact, LogIfTooSlow]
            public void CanBeDeserialized()
            {
                SerializationHelper.CanBeDeserialized(validJson, validSummary);
            }

            [Fact, LogIfTooSlow]
            public void CanBeSerialized()
            {
                SerializationHelper.CanBeSerialized(validJson, validSummary);
            }
        }
    }
}
