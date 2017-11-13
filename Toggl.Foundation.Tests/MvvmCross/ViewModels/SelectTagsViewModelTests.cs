using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.PrimeRadiant.Models;
using Xunit;
using static Toggl.Multivac.Extensions.FunctionalExtensions;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SelectTagsViewModelTests
    {
        public abstract class SelectTagsViewModelTest : BaseViewModelTests<SelectTagsViewModel>
        {
            protected override SelectTagsViewModel CreateViewModel()
                => new SelectTagsViewModel(DataSource, NavigationService);

            protected Task EnsureClosesTheViewModel()
                => NavigationService.Received().Close(Arg.Is(ViewModel), Arg.Any<long[]>());

            protected bool EnsureExpectedTagsAreReturned(long[] actual, long[] expected)
            {
                if (actual.Length != expected.Length) return false;

                foreach (var actualTag in actual)
                {
                    if (!expected.Contains(actualTag))
                        return false;
                }
                return true;
            }
        }

        public sealed class TheConstructor : SelectTagsViewModelTest
        {
            [Theory]
            [ClassData(typeof(TwoParameterConstructorTestData))]
            public void ThrowsIfAnyOfTheArgumentsIsNull(bool useDataSource, bool useNavigationService)
            {
                var dataSource = useDataSource ? DataSource : null;
                var navigationService = useNavigationService ? NavigationService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new SelectTagsViewModel(dataSource, navigationService);

                tryingToConstructWithEmptyParameters
                    .ShouldThrow<ArgumentNullException>();
            }
        }

        public sealed class TheCloseCommand : SelectTagsViewModelTest
        {
            [Fact]
            public async Task ClosesTheViewModel()
            {
                await ViewModel.CloseCommand.ExecuteAsync();

                await EnsureClosesTheViewModel();
            }

            [Fact]
            public async Task ReturnsTheSameTagsThatWerePassedToTheViewModel()
            {
                var tagids = new long[] { 1, 4, 29, 2 };
                ViewModel.Prepare((tagids, 0));

                await ViewModel.CloseCommand.ExecuteAsync();

                await NavigationService
                    .Received()
                    .Close(Arg.Is(ViewModel), Arg.Is<long[]>(tagids));
            }
        }

        public sealed class TheSaveCommand : SelectTagsViewModelTest
        {
            [Fact]
            public async Task ClosesTheViewModel()
            {
                await ViewModel.SaveCommand.ExecuteAsync();

                await EnsureClosesTheViewModel();
            }

            [Fact]
            public async Task ReturnsTheSelectedTagIds()
            {
                var tagIds = Enumerable.Range(0, 20).Select(num => (long)num);
                var selectedTagIds = tagIds.Where(id => id % 2 == 0)
                    .ToArray();
                var selectedTags = selectedTagIds
                    .Select(createDatabaseTagSubstitute)
                    .Select(databaseTag => new TagSuggestion(databaseTag))
                    .Select(tagSuggestion => new SelectableTagViewModel(tagSuggestion, false));
                foreach (var tag in selectedTags)
                    ViewModel.SelectTagCommand.Execute(tag);

                await ViewModel.SaveCommand.ExecuteAsync();

                await NavigationService
                    .Received()
                    .Close(
                        Arg.Is(ViewModel),
                        Arg.Is<long[]>(ids => EnsureExpectedTagsAreReturned(ids, selectedTagIds))
                    );
            }

            [Fact]
            public async Task ReturnsEmptyArrayIfNoTagsWereSelected()
            {
                var expectedIds = new long[0];

                await ViewModel.SaveCommand.ExecuteAsync();

                await NavigationService
                    .Received()
                    .Close(
                        Arg.Is(ViewModel),
                        Arg.Is<long[]>(ids => EnsureExpectedTagsAreReturned(ids, expectedIds))
                    );
            }

            private IDatabaseTag createDatabaseTagSubstitute(long id)
            {
                var tag = Substitute.For<IDatabaseTag>();
                tag.Id.Returns(id);
                tag.Name.Returns($"Tag{id}");
                return tag;
            }
        }

        public sealed class TheTextProperty : SelectTagsViewModelTest
        {
            [Fact]
            public async Task WhenChangedQueriesTheAutocompleteProvider()
            {
                var text = "Some text";
                var autocompleteProvider = Substitute.For<IAutocompleteProvider>();
                DataSource.AutocompleteProvider.Returns(autocompleteProvider);
                await ViewModel.Initialize();

                ViewModel.Text = text;

                await autocompleteProvider.Received()
                    .Query(Arg.Is<QueryInfo>(info
                        => info.Text == text
                        && info.SuggestionType == AutocompleteSuggestionType.Tags));
            }
        }

        public sealed class TheTagsProperty : SelectTagsViewModelTest
        {
            private IEnumerable<TagSuggestion> getTagSuggestions(int count, IDatabaseWorkspace workspace)
            {
                for (int i = 0; i < count; i++)
                {
                    /* Do not inline 'workspace.Id' into another .Return() call 
                     * because it's a proxy that won't work later on!
                     * This must be cached before usage.
                     */
                    var workspaceId = workspace.Id; 

                    var tag = Substitute.For<IDatabaseTag>();
                    tag.Id.Returns(i);
                    tag.WorkspaceId.Returns(workspaceId);
                    tag.Workspace.Returns(workspace);
                    tag.Name.Returns($"Tag{i}");

                    yield return new TagSuggestion(tag);
                }
            }

            private IDatabaseWorkspace createWorkspace(long id, string name)
            {
                var workspace = Substitute.For<IDatabaseWorkspace>();
                workspace.Id.Returns(id);
                workspace.Name.Returns(name);
                return workspace;
            }

            [Fact]
            public async Task OnlyContainsTagsFromTheSameWorkspaceAsTimeEntry()
            {
                var tags = new List<TagSuggestion>();
                var workspaces = Enumerable.Range(0, 5)
                    .Select(i => createWorkspace(i, $"Workspace{i}")).ToArray();
                workspaces.ForEach(workspace
                    => tags.AddRange(getTagSuggestions(5, workspace)));
                var autocompleteProvider = Substitute.For<IAutocompleteProvider>();
                autocompleteProvider
                    .Query(Arg.Is<QueryInfo>(
                        arg => arg.SuggestionType == AutocompleteSuggestionType.Tags))
                    .Returns(Observable.Return(tags));
                DataSource.AutocompleteProvider.Returns(autocompleteProvider);
                var targetWorkspace = workspaces[1];
                DataSource.Workspaces.GetById(Arg.Is(targetWorkspace.Id))
                    .Returns(Observable.Return(targetWorkspace));
                var tagIds = tags.Select(tag => tag.TagId).ToArray();

                ViewModel.Prepare((tagIds, targetWorkspace.Id));
                await ViewModel.Initialize();

                ViewModel.Tags.Should().HaveCount(5);
                ViewModel.Tags.Should()
                    .OnlyContain(tag => tag.Workspace == targetWorkspace.Name);
            }

            [Fact]
            public async Task IsPopulatedAfterInitialization()
            {
                var workspace = createWorkspace(13, "Some workspace");
                var tagSuggestions = getTagSuggestions(10, workspace);
                var tagIds = tagSuggestions.Select(tag => tag.TagId).ToArray();
                var autocompleteProvider = Substitute.For<IAutocompleteProvider>();
                autocompleteProvider
                    .Query(Arg.Is<QueryInfo>(
                        arg => arg.SuggestionType == AutocompleteSuggestionType.Tags))
                    .Returns(Observable.Return(tagSuggestions));
                DataSource.AutocompleteProvider.Returns(autocompleteProvider);
                DataSource.Workspaces.GetById(Arg.Is(workspace.Id))
                    .Returns(Observable.Return(workspace));

                ViewModel.Prepare((tagIds, workspace.Id));
                await ViewModel.Initialize();

                ViewModel.Tags.Should().HaveCount(tagSuggestions.Count());
            }

            [Fact]
            public async Task IsSortedBySelectedStatusThenByName()
            {
                var workspace = createWorkspace(13, "Some workspace");
                var tagSuggestions = getTagSuggestions(4, workspace).ToArray();

                var shuffledTags = new[] { tagSuggestions[3], tagSuggestions[1], tagSuggestions[2], tagSuggestions[0] };
                var selectedTagIds = new[] { tagSuggestions[0].TagId, tagSuggestions[2].TagId };

                var autocompleteProvider = Substitute.For<IAutocompleteProvider>();
                autocompleteProvider
                    .Query(Arg.Is<QueryInfo>(
                        arg => arg.SuggestionType == AutocompleteSuggestionType.Tags))
                    .Returns(Observable.Return(shuffledTags));

                DataSource.AutocompleteProvider.Returns(autocompleteProvider);
                DataSource.Workspaces.GetById(Arg.Is(workspace.Id))
                    .Returns(Observable.Return(workspace));

                ViewModel.Prepare((selectedTagIds, workspace.Id));
                await ViewModel.Initialize();

                ViewModel.Tags.Should().HaveCount(4);

                ViewModel.Tags[0].Name.Should().Be("Tag0");
                ViewModel.Tags[1].Name.Should().Be("Tag2");
                ViewModel.Tags[2].Name.Should().Be("Tag1");
                ViewModel.Tags[3].Name.Should().Be("Tag3");

                ViewModel.Tags[0].Selected.Should().BeTrue();
                ViewModel.Tags[1].Selected.Should().BeTrue();
                ViewModel.Tags[2].Selected.Should().BeFalse();
                ViewModel.Tags[3].Selected.Should().BeFalse();
            }

            [Fact]
            public async Task IsClearedWhenTextIsChanged()
            {
                var workspace = createWorkspace(13, "Some workspace");
                var oldSuggestions = getTagSuggestions(3, workspace);
                var newSuggestions = getTagSuggestions(1, workspace);
                var oldTagIds = oldSuggestions.Select(tag => tag.TagId).ToArray();
                var autocompleteProvider = Substitute.For<IAutocompleteProvider>();
                        var queryText = "Query text";
                autocompleteProvider
                    .Query(Arg.Is<QueryInfo>(
                        arg => arg.SuggestionType == AutocompleteSuggestionType.Tags))
                            .Returns(Observable.Return(oldSuggestions));
                autocompleteProvider
                    .Query(Arg.Is<QueryInfo>(
                        arg => arg.Text == queryText && arg.SuggestionType == AutocompleteSuggestionType.Tags))
                    .Returns(Observable.Return(newSuggestions));
                DataSource.AutocompleteProvider.Returns(autocompleteProvider);
                DataSource.Workspaces.GetById(Arg.Is(workspace.Id))
                    .Returns(Observable.Return(workspace));
                ViewModel.Prepare((oldTagIds, workspace.Id));
                await ViewModel.Initialize();

                ViewModel.Text = queryText;

                ViewModel.Tags.Should().HaveCount(1);
            }
        }

        public sealed class TheSelectTagCommand : SelectTagsViewModelTest
        {
            private TagSuggestion tagSuggestion;

            public TheSelectTagCommand()
            {
                var databaseTag = Substitute.For<IDatabaseTag>();
                databaseTag.Name.Returns("Tag0");
                databaseTag.Id.Returns(0);
                tagSuggestion = new TagSuggestion(databaseTag);
            }

            [Property]
            public void SetsTheSelectedPropertyOnTheTagToTheOpposite(bool initialValue)
            {
                var selectableTag = new SelectableTagViewModel(tagSuggestion, initialValue);

                ViewModel.SelectTagCommand.Execute(selectableTag);

                selectableTag.Selected.Should().Be(!initialValue);
            }

            [Fact]
            public async Task AppendsTheTagIdToSelectedTagIdsIfNotSelectedAlready()
            {
                var selectableTag = new SelectableTagViewModel(tagSuggestion, false);

                ViewModel.SelectTagCommand.Execute(selectableTag);
                await ViewModel.SaveCommand.ExecuteAsync();

                await NavigationService
                    .Received()
                    .Close(
                        Arg.Is(ViewModel),
                        Arg.Is<long[]>(ids => EnsureExpectedTagsAreReturned(ids, new[] { selectableTag.Id }))
                    );
            }

            [Fact]
            public async Task RemovesTheTagIdFromSelectedTagIdsIfSelectedAlready()
            {
                var selectableTag = new SelectableTagViewModel(tagSuggestion, true);
                ViewModel.Prepare((new long[] { selectableTag.Id }, 0));

                ViewModel.SelectTagCommand.Execute(selectableTag);
                await ViewModel.SaveCommand.ExecuteAsync();

                await NavigationService
                    .Received()
                    .Close(
                        Arg.Is(ViewModel),
                        Arg.Is<long[]>(ids => EnsureExpectedTagsAreReturned(ids, new long[0]))
                    );
            }
        }

        public sealed class ThePrepareMethod : SelectTagsViewModelTest
        {
            [Fact]
            public async Task AddsAllPassedTagsToTheSelectedTags()
            {
                var tagIds = new long[] { 100, 3, 10, 34, 532 };

                ViewModel.Prepare((tagIds, 0));
                await ViewModel.SaveCommand.ExecuteAsync();

                await NavigationService
                    .Received()
                    .Close(
                        Arg.Is(ViewModel),
                        Arg.Is<long[]>(ids => EnsureExpectedTagsAreReturned(ids, tagIds))
                    );
            }
        }
    }
}
