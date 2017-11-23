using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using NSubstitute;
using NSubstitute.Core;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.PrimeRadiant.Models;
using Xunit;
using static Toggl.Foundation.Helper.Constants;
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

            protected IEnumerable<TagSuggestion> CreateTags(int count)
                => Enumerable
                    .Range(0, count)
                    .Select(i => CreateTagSubstitute(i, i.ToString()))
                    .Select(tag => new TagSuggestion(tag));

            protected IDatabaseTag CreateTagSubstitute(long id, string name)
            {
                var tag = Substitute.For<IDatabaseTag>();
                tag.Id.Returns(id);
                tag.Name.Returns(name);
                return tag;
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

        public sealed class TheIsEmptyProperty : SelectTagsViewModelTest
        {
            const long workspaceId = 1;
            const long irrelevantWorkspaceId = 2;

            private void setup(Func<long, long> workspaceIdSelector)
            {
                var tags = Enumerable.Range(0, 10)
                                     .Select(i =>
                                     {
                                         var tag = Substitute.For<IDatabaseTag>();
                                         tag.Name.Returns(Guid.NewGuid().ToString());
                                         tag.Id.Returns(i);
                                         tag.WorkspaceId.Returns(workspaceIdSelector(i));
                                         return tag;
                                     })
                                     .ToList();

                var tagsSource = Substitute.For<ITagsSource>();
                tagsSource.GetAll().Returns(Observable.Return(tags));

                DataSource.Tags.Returns(tagsSource);
            }

            [Fact]
            public async Task ReturnsTrueIfHasNoTagsForSelectedWorkspace()
            {
                setup(i => irrelevantWorkspaceId);

                ViewModel.Prepare((new long[] { }, workspaceId));
                await ViewModel.Initialize();

                ViewModel.IsEmpty.Should().BeTrue();
            }

            [Fact]
            public async Task ReturnsFalseIfTagsForWorkspaceExist()
            {
                setup(i => i % 2 == 0 ? irrelevantWorkspaceId : workspaceId);

                ViewModel.Prepare((new long[] { }, workspaceId));
                await ViewModel.Initialize();

                ViewModel.IsEmpty.Should().BeFalse();
            }

            [Fact]
            public async Task ReturnsFalseIfTagsForWorkspaceExistButFilteredCollectionIsEmpty()
            {
                setup(i => i % 2 == 0 ? irrelevantWorkspaceId : workspaceId);

                var autocompleteProvider = Substitute.For<IAutocompleteProvider>();

                autocompleteProvider
                    .Query(Arg.Is<QueryInfo>(
                        arg => arg.SuggestionType == AutocompleteSuggestionType.Tags))
                    .Returns(Observable.Return(new List<TagSuggestion>()));

                ViewModel.Prepare((new long[] { }, workspaceId));
                await ViewModel.Initialize();

                ViewModel.Text = "Anything";

                ViewModel.IsEmpty.Should().BeFalse();
            }

            [Fact]
            public async Task ReturnsFalseIfTagIsCreated()
            {
                var tagsSource = Substitute.For<ITagsSource>();
                tagsSource.GetAll().Returns(Observable.Return(new List<IDatabaseTag>()));

                var newTag = Substitute.For<IDatabaseTag>();
                newTag.Id.Returns(12345);

                tagsSource
                    .Create(Arg.Any<string>(), Arg.Any<long>())
                    .Returns(Observable.Return(newTag));

                DataSource.Tags.Returns(tagsSource);

                ViewModel.Prepare((new long[] { }, workspaceId));
                await ViewModel.Initialize();

                ViewModel.CreateTagCommand.Execute("some-tag");

                ViewModel.IsEmpty.Should().BeFalse();
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

        public sealed class TheCreateTagMethod : SelectTagsViewModelTest
        {
            private async Task prepare(long tagId, long workspaceId)
            {
                ViewModel.Prepare((new long[0], workspaceId));
                await ViewModel.Initialize();

                var createdTag = Substitute.For<IDatabaseTag>();
                createdTag.Id.Returns(tagId);
                createdTag.WorkspaceId.Returns(workspaceId);
                DataSource
                    .Tags
                    .Create(Arg.Any<string>(), Arg.Any<long>())
                    .Returns(Observable.Return(createdTag));
                
                var observable = Observable
                    .Return(new AutocompleteSuggestion[] { new TagSuggestion(createdTag) });
                DataSource
                    .AutocompleteProvider
                    .Query(Arg.Any<QueryInfo>())
                    .Returns(observable);
            }

            [Property]
            public void CreatesNewTagWithProvidedName(NonEmptyString nonEmptyString)
            {
                var name = nonEmptyString.Get.Trim();
                prepare(420, 10).Wait();
                ViewModel.Text = name;

                ViewModel.CreateTagCommand.ExecuteAsync(name).Wait();

                DataSource.Tags
                    .Received()
                    .Create(Arg.Is(name), Arg.Any<long>())
                    .Wait();
            }

            [Property]
            public void CreatesNewTagWithWorkspaceOfViewModel(long workspaceId)
            {
                prepare(240, workspaceId).Wait();
                var tagName = "some tag";
                ViewModel.Text = tagName;

                ViewModel.CreateTagCommand.ExecuteAsync(tagName).Wait();

                DataSource
                    .Tags
                    .Received()
                    .Create(Arg.Any<string>(), Arg.Is(workspaceId))
                    .Wait();
            }

            [Property]
            public void ClearsTheText(NonEmptyString initialText)
            {
                prepare(123, 45).Wait();
                ViewModel.Text = initialText.Get;

                ViewModel.CreateTagCommand.ExecuteAsync("Some text").Wait();

                ViewModel.Text.Should().BeEmpty();
            }

            [Fact]
            public void PrependsTheNewTagToTheTagList()
            {
                var tagName = "Some Tag";
                long workspaceId = 123;
                var initialTags = CreateTags(count: 10);
                DataSource.AutocompleteProvider.Query(Arg.Any<QueryInfo>())
                    .Returns(Observable.Return(initialTags));
                var createdTag = CreateTagSubstitute(0, tagName);
                createdTag.WorkspaceId.Returns(workspaceId);
                DataSource.Tags.Create(Arg.Is(tagName), Arg.Any<long>())
                    .Returns(Observable.Return(createdTag));
                ViewModel.Prepare((new long[0], workspaceId));
                ViewModel.Initialize().Wait();
                //Reconfigure AutocopleteProvider, so it returns the created tag as well
                var createdTagSuggestion = new TagSuggestion(createdTag);
                var newTagSuggestions = initialTags
                    .Concat(new AutocompleteSuggestion[] { createdTagSuggestion });
                var observable = Observable
                    .Return(newTagSuggestions);
                DataSource
                    .AutocompleteProvider
                    .Query(Arg.Any<QueryInfo>())
                    .Returns(observable);
                ViewModel.Text = tagName;

                ViewModel.CreateTagCommand.ExecuteAsync(tagName).Wait();

                ViewModel.Tags.First().Name.Should().Be(tagName);
            }

            [Fact]
            public void SelectsTheNewTag()
            {
                prepare(10, 20).Wait();
                var name = "Tag";
                ViewModel.Text = name;

                ViewModel.CreateTagCommand.ExecuteAsync(name).Wait();

                ViewModel.Tags.First().Selected.Should().BeTrue();
            }

            [Theory]
            [InlineData("   Some tag", "Some tag")]
            [InlineData("Some tag   ", "Some tag")]
            [InlineData("   Some tag   ", "Some tag")]
            [InlineData("\tSome tag", "Some tag")]
            [InlineData("Some tag\t", "Some tag")]
            [InlineData("\tSome tag\t", "Some tag")]
            [InlineData("\t   Some tag  \t  ", "Some tag")]
            public async Task TrimsTheTagName(string text, string expectedName)
            {
                await prepare(12, 13);
                ViewModel.Text = text;

                await ViewModel.CreateTagCommand.ExecuteAsync(text);

                await DataSource
                    .Tags
                    .Received()
                    .Create(Arg.Is(expectedName), Arg.Any<long>());
            }
        }

        public sealed class TheSuggestCreationproperty : SelectTagsViewModelTest
        {
            [Theory]
            [InlineData("")]
            [InlineData("     ")]
            [InlineData("\t")]
            [InlineData("  \t  ")]
            public void IsFalseWhenTextIsEmptyOrWhiteSpace(string text)
            {
                ViewModel.Text = text;

                ViewModel.SuggestCreation.Should().BeFalse();
            }

            [Theory]
            [InlineData("1")]
            [InlineData("4")]
            [InlineData("  6    ")]
            [InlineData("\t7  ")]
            public async Task IsFalseWhenSuchTagAlreadyExists(string text)
            {
                var tags = CreateTags(10);
                DataSource
                    .AutocompleteProvider
                    .Query(Arg.Any<QueryInfo>())
                    .Returns(Observable.Return(tags));
                ViewModel.Prepare((new long[0], 0));
                await ViewModel.Initialize();

                ViewModel.Text = text;

                ViewModel.SuggestCreation.Should().BeFalse();
            }

            [Theory]
            [InlineData("c", MaxTagNameLengthInBytes + 1)]
            [InlineData("Ж", MaxTagNameLengthInBytes / 2 + 1)]
            [InlineData("🍔", MaxTagNameLengthInBytes / 4 + 1)]
            public void IsFalseWhenTextLengthExceedsMaxLength(string grapheme, int length)
            {
                ViewModel.Text = getLongTagName(grapheme, length);

                ViewModel.SuggestCreation.Should().BeFalse();
            }

            [Theory]
            [InlineData("Some tag")]
            [InlineData("  \t Some tag  \t")]
            public void IsTrueWhenAllConditionsAreMet(string text)
            {
                ViewModel.Text = text;

                ViewModel.SuggestCreation.Should().BeTrue();
            }

            private static string getLongTagName(string grapheme, int length)
                => Enumerable
                    .Range(0, length)
                    .Aggregate(
                        new StringBuilder(),
                        (builder, _) => builder.Append(grapheme))
                    .ToString();
        }
    }
}
