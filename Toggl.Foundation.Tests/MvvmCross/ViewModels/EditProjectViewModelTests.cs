using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MvvmCross.Platform.UI;
using NSubstitute;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.Multivac;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class EditProjectViewModelTests
    {
        public abstract class EditProjectViewModelTest : BaseViewModelTests<EditProjectViewModel>
        {
            protected const long WorkspaceId = 10;
            protected const string WorkspaceName = "Some workspace name";
            protected IDatabaseWorkspace Workspace { get; } = Substitute.For<IDatabaseWorkspace>();

            protected EditProjectViewModelTest()
            {
                ViewModel.Name = "A valid name";
            }

            protected override EditProjectViewModel CreateViewModel()
                => new EditProjectViewModel(DataSource, NavigationService);
        }

        public sealed class TheConstructor : EditProjectViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ClassData(typeof(TwoParameterConstructorTestData))]
            public void ThrowsIfAnyOfTheArgumentsIsNull(bool useDataSource, bool useNavigationService)
            {
                var dataSource = useDataSource ? DataSource : null;
                var navigationService = useNavigationService ? NavigationService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new EditProjectViewModel(dataSource, navigationService);

                tryingToConstructWithEmptyParameters
                    .ShouldThrow<ArgumentNullException>();
            }
        }

        public sealed class TheSaveEnabledProperty : EditProjectViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void IsFalseWhenTheNameIsEmpty()
            {
                ViewModel.Name = "";

                ViewModel.SaveEnabled.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void IsFalseWhenTheNameIsJustWhiteSpace()
            {
                ViewModel.Name = "            ";

                ViewModel.SaveEnabled.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void IsFalseWhenTheNameIsLongerThanTheThresholdInBytes()
            {
                ViewModel.Name = "This is a ridiculously big project name made solely with the purpose of testing whether or not Toggl apps UI has validation logic that prevents such a large name to be persisted or, even worse, pushed to the api, an event that might end up in crashes and whatnot";

                ViewModel.SaveEnabled.Should().BeFalse();
            }
        }

        public sealed class TheInitializeMethod : EditProjectViewModelTest
        {
            public TheInitializeMethod()
            {
                DataSource.Workspaces
                    .GetDefault()
                    .Returns(Observable.Return(Workspace));

                Workspace.Id.Returns(WorkspaceId);
                Workspace.Name.Returns(WorkspaceName);

                ViewModel.Prepare("Some name");
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheWorkspaceId()
            {
                await ViewModel.Initialize();
                await ViewModel.DoneCommand.ExecuteAsync();

                await DataSource.Projects
                    .Received()
                    .Create(Arg.Is<CreateProjectDTO>(dto => dto.WorkspaceId == WorkspaceId));
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheWorkspaceName()
            {
                await ViewModel.Initialize();

                ViewModel.WorkspaceName.Should().Be(WorkspaceName);
            }
        }

        public sealed class TheCloseCommand : EditProjectViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                await ViewModel.CloseCommand.ExecuteAsync();

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Any<long?>());
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsNull()
            {
                ViewModel.Prepare("Some name");

                await ViewModel.CloseCommand.ExecuteAsync();

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Is<long?>(result => result == null));
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotTrySavingTheChanges()
            {
                ViewModel.Prepare("Some name");

                await ViewModel.CloseCommand.ExecuteAsync();

                await DataSource.Projects.DidNotReceive().Create(Arg.Any<CreateProjectDTO>());
            }
        }

        public sealed class TheDoneCommand : EditProjectViewModelTest
        {
            private const long proWorkspaceId = 11;
            private const long projectId = 12;

            private readonly IDatabaseProject project = Substitute.For<IDatabaseProject>();

            public TheDoneCommand()
            {
                DataSource.Workspaces
                    .WorkspaceHasFeature(WorkspaceId, WorkspaceFeatureId.Pro)
                    .Returns(Observable.Return(false));

                DataSource.Workspaces
                    .WorkspaceHasFeature(proWorkspaceId, WorkspaceFeatureId.Pro)
                    .Returns(Observable.Return(true));

                DataSource.Workspaces
                    .GetDefault()
                    .Returns(Observable.Return(Workspace));

                DataSource.Workspaces
                    .GetById(Arg.Any<long>())
                    .Returns(Observable.Return(Workspace));

                DataSource.Projects
                    .Create(Arg.Any<CreateProjectDTO>())
                    .Returns(Observable.Return(project));

                project.Id.Returns(projectId);
                Workspace.Id.Returns(proWorkspaceId);
            }

            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                ViewModel.Prepare("Some name");

                await ViewModel.DoneCommand.ExecuteAsync();

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Any<long?>());
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTheIdOfTheCreatedProject()
            {
                ViewModel.Prepare("Some name");

                await ViewModel.DoneCommand.ExecuteAsync();

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Is(projectId));
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotCallCreateIfTheProjectNameIsInvalid()
            {
                ViewModel.Prepare("Some name");
                ViewModel.Name = "";

                await ViewModel.DoneCommand.ExecuteAsync();

                await DataSource.Projects.DidNotReceive()
                    .Create(Arg.Any<CreateProjectDTO>());
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotCloseTheViewModelIfTheProjectNameIsInvalid()
            {
                ViewModel.Prepare("Some name");
                ViewModel.Name = "";

                await ViewModel.DoneCommand.ExecuteAsync();

                await NavigationService.DidNotReceive()
                    .Close(ViewModel, projectId);
            }

            [Fact, LogIfTooSlow]
            public async Task SetsBillableToNullIfTheWorkspaceIfNotPro()
            {
                Workspace.Id.Returns(WorkspaceId);
                ViewModel.Prepare("Some name");

                await ViewModel.DoneCommand.ExecuteAsync();

                await DataSource.Projects.Received().Create(
                    Arg.Is<CreateProjectDTO>(dto => dto.Billable == null)
                );
            }

            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public async Task SetsBillableToTheValueOfTheProjectsBillableByDefaultPropertyIfTheWorkspaceIsPro(
                bool billableByDefault)
            {
                Workspace.Id.Returns(proWorkspaceId);
                Workspace.ProjectsBillableByDefault.Returns(billableByDefault);
                ViewModel.Prepare("Some name");
                await ViewModel.Initialize();

                await ViewModel.DoneCommand.ExecuteAsync();

                await DataSource.Projects.Received().Create(
                    Arg.Is<CreateProjectDTO>(dto => dto.Billable == billableByDefault)
                );
            }

            [Theory, LogIfTooSlow]
            [InlineData("   abcde", "abcde")]
            [InlineData("abcde     ", "abcde")]
            [InlineData("  abcde ", "abcde")]
            [InlineData("abcde  fgh", "abcde  fgh")]
            [InlineData("      abcd\nefgh     ", "abcd\nefgh")]
            public async Task TrimsNameFromTheStartAndTheEndBeforeSaving(string name, string trimmed)
            {
                ViewModel.Prepare(name);
                await ViewModel.Initialize();

                await ViewModel.DoneCommand.ExecuteAsync();

                await DataSource.Projects.Received().Create(
                    Arg.Is<CreateProjectDTO>(dto => dto.Name == trimmed)
                );
            }
        }

        public sealed class ThePickColorCommand : EditProjectViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task CallsTheSelectColorViewModel()
            {
                ViewModel.Prepare("Some name");

                await ViewModel.PickColorCommand.ExecuteAsync();

                await NavigationService.Received()
                    .Navigate<ColorParameters, MvxColor>(typeof(SelectColorViewModel), Arg.Any<ColorParameters>());
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheReturnedColorAsTheColorProperty()
            {
                var expectedColor = MvxColors.AliceBlue;
                NavigationService
                    .Navigate<ColorParameters, MvxColor>(typeof(SelectColorViewModel), Arg.Any<ColorParameters>())
                    .Returns(Task.FromResult(expectedColor));
                ViewModel.Prepare("Some name");
                ViewModel.Color = MvxColors.Azure;

                await ViewModel.PickColorCommand.ExecuteAsync();

                ViewModel.Color.ARGB.Should().Be(expectedColor.ARGB);
            }
        }

        public sealed class ThePickWorkspaceCommand : EditProjectViewModelTest
        {
            private const long workspaceId = 10;
            private const long defaultWorkspaceId = 11;
            private const string workspaceName = "My custom workspace";
            private readonly IDatabaseWorkspace workspace = Substitute.For<IDatabaseWorkspace>();
            private readonly IDatabaseWorkspace defaultWorkspace = Substitute.For<IDatabaseWorkspace>();

            public ThePickWorkspaceCommand()
            {
                workspace.Id.Returns(workspaceId);
                workspace.Name.Returns(workspaceName);
                defaultWorkspace.Id.Returns(defaultWorkspaceId);

                DataSource.Workspaces.GetDefault()
                    .Returns(Observable.Return(defaultWorkspace));
                
                DataSource.Workspaces.GetById(workspaceId)
                    .Returns(Observable.Return(workspace));

                ViewModel.Prepare();
            }

            [Fact, LogIfTooSlow]
            public async Task CallsTheSelectWorkspaceViewModel()
            {
                await ViewModel.PickWorkspaceCommand.ExecuteAsync();

                await NavigationService.Received()
                    .Navigate<WorkspaceParameters, long>(typeof(SelectWorkspaceViewModel), Arg.Any<WorkspaceParameters>());
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheReturnedWorkspaceNameAsTheWorkspaceNameProperty()
            {
                NavigationService
                    .Navigate<WorkspaceParameters, long>(typeof(SelectWorkspaceViewModel), Arg.Any<WorkspaceParameters>())
                    .Returns(Task.FromResult(workspaceId));

                await ViewModel.PickWorkspaceCommand.ExecuteAsync();

                ViewModel.WorkspaceName.Should().Be(workspaceName);
            }

            [Fact, LogIfTooSlow]
            public async Task ResetsTheClientNameWhenTheWorkspaceChanges()
            {
                NavigationService
                    .Navigate<WorkspaceParameters, long>(typeof(SelectWorkspaceViewModel), Arg.Any<WorkspaceParameters>())
                    .Returns(Task.FromResult(workspaceId));

                await ViewModel.PickWorkspaceCommand.ExecuteAsync();

                ViewModel.ClientName.Should().BeNullOrEmpty();
            }

            [Fact, LogIfTooSlow]
            public async Task PicksADefaultColorIfTheSelectedColorIsCustomAndTheWorkspaceIsNotPro()
            {
                NavigationService
                    .Navigate<MvxColor, MvxColor>(typeof(SelectColorViewModel), Arg.Any<MvxColor>())
                    .Returns(Task.FromResult(MvxColors.Azure));
                NavigationService
                    .Navigate<WorkspaceParameters, long>(typeof(SelectWorkspaceViewModel), Arg.Any<WorkspaceParameters>())
                    .Returns(Task.FromResult(workspaceId));
                DataSource.Workspaces.WorkspaceHasFeature(workspaceId, WorkspaceFeatureId.Pro)
                    .Returns(Observable.Return(false));
                await ViewModel.PickColorCommand.ExecuteAsync();

                await ViewModel.PickWorkspaceCommand.ExecuteAsync();

                ViewModel.Color.Should().NotBe(MvxColors.Azure);
            }
        }

        public sealed class ThePickClientCommand : EditProjectViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task CallsTheSelectClientViewModel()
            {
                ViewModel.Prepare("Some name");

                await ViewModel.PickClientCommand.ExecuteAsync();

                await NavigationService.Received()
                    .Navigate<long, long?>(typeof(SelectClientViewModel), Arg.Any<long>());
            }

            [Fact, LogIfTooSlow]
            public async Task PassesTheCurrentWorkspaceToTheViewModel()
            {
                DataSource.Workspaces
                    .GetDefault()
                    .Returns(Observable.Return(Workspace));
                Workspace.Id.Returns(WorkspaceId);
                ViewModel.Prepare("Some name");
                await ViewModel.Initialize();

                await ViewModel.PickClientCommand.ExecuteAsync();

                await NavigationService.Received()
                    .Navigate<long, long?>(typeof(SelectClientViewModel), WorkspaceId);
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheReturnedClientAsTheClientNameProperty()
            {
                const string expectedName = "Some client";
                long? expectedId = 10;
                var client = Substitute.For<IDatabaseClient>();
                client.Id.Returns(expectedId.Value);
                client.Name.Returns(expectedName);
                NavigationService
                    .Navigate<long, long?>(typeof(SelectClientViewModel), Arg.Any<long>())
                    .Returns(Task.FromResult(expectedId));
                DataSource.Workspaces
                    .GetDefault()
                    .Returns(Observable.Return(Workspace));
                DataSource.Clients.GetById(expectedId.Value).Returns(Observable.Return(client));
                Workspace.Id.Returns(WorkspaceId);
                ViewModel.Prepare("Some name");

                await ViewModel.PickClientCommand.ExecuteAsync();

                ViewModel.ClientName.Should().Be(expectedName);
            }

            [Fact, LogIfTooSlow]
            public async Task ClearsTheCurrentClientIfZeroIsReturned()
            {
                const string expectedName = "Some client";
                long? expectedId = 10;
                var client = Substitute.For<IDatabaseClient>();
                client.Id.Returns(expectedId.Value);
                client.Name.Returns(expectedName);
                NavigationService
                    .Navigate<long, long?>(typeof(SelectClientViewModel), Arg.Any<long>())
                    .Returns(Task.FromResult(expectedId));
                DataSource.Workspaces
                    .GetDefault()
                    .Returns(Observable.Return(Workspace));
                DataSource.Clients.GetById(expectedId.Value).Returns(Observable.Return(client));
                Workspace.Id.Returns(WorkspaceId);
                ViewModel.Prepare("Some name");
                await ViewModel.PickClientCommand.ExecuteAsync();
                NavigationService
                    .Navigate<long, long?>(typeof(SelectClientViewModel), Arg.Any<long>())
                    .Returns(Task.FromResult<long?>(0));

                await ViewModel.PickClientCommand.ExecuteAsync();

                ViewModel.ClientName.Should().BeNullOrEmpty();
            }
        }
    }
}
