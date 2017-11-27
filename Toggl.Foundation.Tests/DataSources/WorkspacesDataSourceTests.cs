using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.DataSources;
using Toggl.Multivac;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.DataSources
{
    public sealed class WorkspacesDataSourceTests
    {
        public abstract class WorkspaceDataSourceTest
        {
            protected const int DefaultWorkspaceId = 10;

            protected IDatabaseUser User { get; } = Substitute.For<IDatabaseUser>();

            protected IDatabaseWorkspace DefaultWorkspace { get; } = Substitute.For<IDatabaseWorkspace>();

            protected ITogglDatabase Database { get; } = Substitute.For<ITogglDatabase>();

            protected IWorkspacesSource WorkspacesSource { get; }

            protected WorkspaceDataSourceTest()
            {
                WorkspacesSource = new WorkspacesDataSource(Database);

                User.DefaultWorkspaceId.Returns(DefaultWorkspaceId);
            }
        }

        public sealed class TheConstructor : WorkspaceDataSourceTest
        {
            [Fact, LogIfTooSlow]
            public void ThrowsIfTheArgumentIsNull()
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new WorkspacesDataSource(null);

                tryingToConstructWithEmptyParameters
                    .ShouldThrow<ArgumentNullException>();
            }
        }

        public sealed class TheGetDefaultMethod : WorkspaceDataSourceTest
        {
            [Fact, LogIfTooSlow]
            public async Task ReturnsTheDefaultWorkspaceForTheCurrentUser()
            {
                Database.User.Single().Returns(Observable.Return(User));
                Database.Workspaces.GetById(DefaultWorkspaceId).Returns(Observable.Return(DefaultWorkspace));

                var defaultWorkspace = await WorkspacesSource.GetDefault();

                defaultWorkspace.Should().BeSameAs(DefaultWorkspace);
            }
        }

        public sealed class TheGetByIdMethod : WorkspaceDataSourceTest
        {
            [Fact, LogIfTooSlow]
            public async Task ReturnsTheWorkspaceWithPassedId()
            {
                Database.Workspaces.GetById(DefaultWorkspaceId).Returns(Observable.Return(DefaultWorkspace));

                var defaultWorkspace = await WorkspacesSource.GetById(DefaultWorkspaceId);

                defaultWorkspace.Should().BeSameAs(DefaultWorkspace);
            }
        }

        public sealed class TheWorkspaceHasFeatureMethod : WorkspaceDataSourceTest
        {
            [Fact, LogIfTooSlow]
            public async Task IndicatesWhetherTheUserHasAFeatureAvailableOrNot()
            {
                var featureCollection = Substitute.For<IDatabaseWorkspaceFeatureCollection>();
                featureCollection.IsEnabled(WorkspaceFeatureId.Pro).Returns(true);
                Database.WorkspaceFeatures.GetById(DefaultWorkspaceId).Returns(Observable.Return(featureCollection));

                var isEnabled = await WorkspacesSource.WorkspaceHasFeature(DefaultWorkspaceId, WorkspaceFeatureId.Pro);

                isEnabled.Should().BeTrue();
            }
        }
    }
}
