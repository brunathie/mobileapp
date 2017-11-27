using System;
using FluentAssertions;
using Toggl.Foundation.DataSources;
using Xunit;

namespace Toggl.Foundation.Tests.DataSources
{
    public sealed class TasksDataSourceTests
    {
        public sealed class TheConstructor
        {
            [Fact, LogIfTooSlow]
            public void ThrowsIfTheArgumentIsNull()
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new TasksDataSource(null);

                tryingToConstructWithEmptyParameters
                    .ShouldThrow<ArgumentNullException>();
            }
        }
    }
}
