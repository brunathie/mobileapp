using System.Collections.Generic;
using Toggl.Ultrawave.ReportsModels;
using Xunit;

namespace Toggl.Ultrawave.Tests.Models
{
    public sealed class ProjectsSummaryTests
    {
        public sealed class TheProjectsSummaryModel
        {
            private string validJson
                => "[{\"user_id\":23741667,\"project_id\":1427273,\"tracked_seconds\":9876,\"billed_seconds\":6543},"
                    + "{\"user_id\":23741667,\"project_id\":1427273,\"tracked_seconds\":5678,\"billed_seconds\":null},"
                    + "{\"user_id\":23741667,\"project_id\":1427273,\"tracked_seconds\":4598,\"billed_seconds\":56}]";

            private List<ProjectSummary> validSummary => new List<ProjectSummary>
            {
                new ProjectSummary
                {
                    UserId = 23741667,
                    ProjectId = 1427273,
                    TrackedSeconds = 9876,
                    BilledSeconds = 6543
                },
                new ProjectSummary
                {
                    UserId = 23741667,
                    ProjectId = 1427273,
                    TrackedSeconds = 5678,
                    BilledSeconds = null
                },
                new ProjectSummary
                {
                    UserId = 23741667,
                    ProjectId = 1427273,
                    TrackedSeconds = 4598,
                    BilledSeconds = 56
                }
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
