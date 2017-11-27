using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.Autocomplete
{
    public sealed class AutocompleteProviderTests
    {
        public abstract class AutocompleteProviderTest
        {
            protected const long WorkspaceId = 9;
            protected const long ProjectId = 10;
            protected const string ProjectName = "Toggl";
            protected const string ProjectColor = "#F41F19";
            protected const string Description = "Testing Toggl mobile apps";

            protected AutocompleteProvider Provider { get; }

            protected ITogglDatabase Database { get; } = Substitute.For<ITogglDatabase>();

            protected IEnumerable<IDatabaseTag> Tags { get; }
            protected IEnumerable<IDatabaseTask> Tasks { get; }
            protected IEnumerable<IDatabaseClient> Clients { get; }
            protected IEnumerable<IDatabaseProject> Projects { get; }
            protected IEnumerable<IDatabaseTimeEntry> TimeEntries { get; }

            protected AutocompleteProviderTest()
            {
                Provider = new AutocompleteProvider(Database);

                Clients = Enumerable.Range(10, 10).Select(id =>
                {
                    var client = Substitute.For<IDatabaseClient>();
                    client.Id.Returns(id);
                    client.Name.Returns(id.ToString());
                    return client;
                });

                Tasks = Enumerable.Range(20, 10).Select(id =>
                {
                    var task = Substitute.For<IDatabaseTask>();
                    task.Id.Returns(id);
                    task.Name.Returns(id.ToString());
                    return task;
                }).ToList();

                Projects = Enumerable.Range(30, 10).Select(id =>
                {
                    var tasks = id % 2 == 0 ? Tasks.Where(t => (t.Id == id - 10 || t.Id == id - 11)).ToList() : null;
                    var project = Substitute.For<IDatabaseProject>();
                    project.Id.Returns(id);
                    project.Name.Returns(id.ToString());
                    project.Color.Returns("#1e1e1e");
                    project.Tasks.Returns(tasks);

                    var client = id % 2 == 0 ? Clients.Single(c => c.Id == id - 20) : null;
                    project.Client.Returns(client);

                    if (tasks != null)
                    {
                        foreach (var task in tasks)
                            task.ProjectId.Returns(id);
                    }

                    return project;
                });

                TimeEntries = Enumerable.Range(40, 10).Select(id =>
                {
                    var timeEntry = Substitute.For<IDatabaseTimeEntry>();
                    timeEntry.Id.Returns(id);
                    timeEntry.Description.Returns(id.ToString());

                    var project = id % 2 == 0 ? Projects.Single(c => c.Id == id - 10) : null;
                    timeEntry.Project.Returns(project);

                    var task = id > 45 ? project?.Tasks?.First() : null;
                    //var task = id % 2 == 1 ? Tasks.Single(t => t.Id == id - 20) : null;
                    timeEntry.Task.Returns(task);

                    return timeEntry;
                });

                Tags = Enumerable.Range(50, 10).Select(id =>
                {
                    var tag = Substitute.For<IDatabaseTag>();
                    tag.Id.Returns(id);
                    tag.Name.Returns(id.ToString());
                    return tag;
                });

                Database.Tasks.GetAll()
                    .Returns(callInfo => Observable.Return(Tasks));

                Database.Tags.GetAll()
                    .Returns(callInfo => Observable.Return(Tags));

                Database.Projects.GetAll()
                    .Returns(callInfo => Observable.Return(Projects));

                Database.TimeEntries.GetAll()
                    .Returns(callInfo => Observable.Return(TimeEntries));
            }
        }

        public sealed class TheConstructor
        {
            [Fact, LogIfTooSlow]
            public void ThrowsIfTheArgumentIsNull()
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new AutocompleteProvider(null);

                tryingToConstructWithEmptyParameters
                    .ShouldThrow<ArgumentNullException>();
            }
        }

        public sealed class TheQueryMethod
        {
            public sealed class QueriesTheDatabaseForTimeEntries : AutocompleteProviderTest
            {
                [Theory, LogIfTooSlow]
                [InlineData("Nothing")]
                [InlineData("Testing Toggl mobile apps")]
                public async Task WhenTheUserBeginsTypingADescription(string description)
                {
                    var textFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, 0);

                    await Provider.Query(textFieldInfo);

                    await Database.TimeEntries.Received().GetAll();
                }

                [Theory, LogIfTooSlow]
                [InlineData("Nothing")]
                [InlineData("Testing Toggl mobile apps")]
                public async Task WhenTheUserHasTypedAnySearchSymbolsButMovedTheCaretToAnIndexThatComesBeforeTheSymbol(
                    string description)
                {
                    var actualDescription = $"{description}@{description}";
                    var textFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(actualDescription, 0);

                    await Provider.Query(textFieldInfo);

                    await Database.TimeEntries.Received().GetAll();
                }

                [Fact, LogIfTooSlow]
                public async Task WhenTheUserHasAlreadySelectedAProjectAndTypesTheAtSymbol()
                {
                    var description = $"Testing Mobile Apps @toggl";
                    var textFieldInfo = TextFieldInfo.Empty
                        .WithTextAndCursor(description, description.Length)
                        .WithProjectInfo(WorkspaceId, ProjectId, ProjectName, ProjectColor);
                    
                    await Provider.Query(textFieldInfo);

                    await Database.TimeEntries.Received().GetAll();
                }

                [Fact, LogIfTooSlow]
                public async Task SearchesTheDescription()
                {
                    const string description = "40";
                    var textFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, 0);

                    var suggestions = await Provider.Query(textFieldInfo);

                    await Database.TimeEntries.Received().GetAll();
                    suggestions.Should().HaveCount(1)
                        .And.AllBeOfType<TimeEntrySuggestion>();
                }

                [Fact, LogIfTooSlow]
                public async Task SearchesTheProjectsName()
                {
                    const string description = "30";
                    var textFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, 0);

                    var suggestions = await Provider.Query(textFieldInfo);

                    await Database.TimeEntries.Received().GetAll();
                    suggestions.Should().HaveCount(1)
                        .And.AllBeOfType<TimeEntrySuggestion>();
                }

                [Fact, LogIfTooSlow]
                public async Task SearchesTheClientsName()
                {
                    const string description = "10";
                    var textFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, 0);

                    var suggestions = await Provider.Query(textFieldInfo);

                    await Database.TimeEntries.Received().GetAll();
                    suggestions.Should().HaveCount(1)
                        .And.AllBeOfType<TimeEntrySuggestion>();
                }

                [Fact, LogIfTooSlow]
                public async Task SearchesTheTaskName()
                {
                    const string description = "25";
                    var textFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, 0);

                    var suggestions = await Provider.Query(textFieldInfo);

                    await Database.TimeEntries.Received().GetAll();
                    suggestions.Should().HaveCount(1)
                        .And.AllBeOfType<TimeEntrySuggestion>();
                    var suggestion = (TimeEntrySuggestion)suggestions.First();
                    suggestion.TaskId.Should().Be(25);
                    suggestion.ProjectId.Should().Be(36);
                    suggestion.Description.Should().Be("46");
                }

                [Fact, LogIfTooSlow]
                public async Task OnlyDisplaysResultsTheHaveHasAtLeastOneMatchOnEveryWordTyped()
                {
                    const string description = "10 30 4";
                    var textFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, 0);

                    var suggestions = await Provider.Query(textFieldInfo);

                    await Database.TimeEntries.Received().GetAll();
                    suggestions.Should().HaveCount(1)
                        .And.AllBeOfType<TimeEntrySuggestion>();
                }
            }

            public sealed class QueriesTheDatabaseForProjects : AutocompleteProviderTest
            {
                [Theory, LogIfTooSlow]
                [InlineData("Nothing")]
                [InlineData("Testing Toggl mobile apps")]
                public async Task WhenTheAtSymbolIsTyped(string description)
                {
                    var actualDescription = $"{description}@{description}";
                    var textFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(actualDescription, description.Length + 1);

                    await Provider.Query(textFieldInfo);

                    await Database.Projects.Received().GetAll();
                }

                [Fact, LogIfTooSlow]
                public async Task SearchesTheName()
                {
                    const string description = "@30";
                    var textFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, 1);

                    var suggestions = await Provider.Query(textFieldInfo);

                    await Database.Projects.Received().GetAll();
                    suggestions.Should().HaveCount(1)
                        .And.AllBeOfType<ProjectSuggestion>();
                }

                [Fact, LogIfTooSlow]
                public async Task SearchesTheClientsName()
                {
                    const string description = "@10";
                    var textFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, 1);

                    var suggestions = await Provider.Query(textFieldInfo);

                    await Database.Projects.Received().GetAll();
                    suggestions.Should().HaveCount(1)
                        .And.AllBeOfType<ProjectSuggestion>();
                }

                [Fact, LogIfTooSlow]
                public async Task SearchesTheTaskName()
                {
                    const string description = "@20";
                    var textFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, 1);

                    var suggestions = await Provider.Query(textFieldInfo);

                    await Database.Projects.Received().GetAll();
                    suggestions.Should().HaveCount(1)
                        .And.AllBeOfType<ProjectSuggestion>();
                }

                [Fact, LogIfTooSlow]
                public async Task OnlyDisplaysResultsTheHaveHasAtLeastOneMatchOnEveryWordTyped()
                {
                    const string description = "@10 3";
                    var textFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, 1);

                    var suggestions = await Provider.Query(textFieldInfo);

                    await Database.Projects.Received().GetAll();
                    suggestions.Should().HaveCount(1)
                        .And.AllBeOfType<ProjectSuggestion>();
                }
            }

            public sealed class QueriesTheDatabaseForTags : AutocompleteProviderTest
            {
                [Theory, LogIfTooSlow]
                [InlineData("Nothing")]
                [InlineData("Testing Toggl mobile apps")]
                public async Task WhenTheHashtagSymbolIsTyped(string description)
                {
                    var actualDescription = $"{description}#{description}";
                    var textFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(actualDescription, description.Length + 1);

                    await Provider.Query(textFieldInfo);

                    await Database.Tags.Received().GetAll();
                }

                [Fact, LogIfTooSlow]
                public async Task SuggestsAllTagsWhenThereIsNoStringToSearch()
                {
                    const string description = "#";
                    var textFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, 1);

                    var suggestions = await Provider.Query(textFieldInfo);

                    await Database.Tags.Received().GetAll();
                    suggestions.Should().HaveCount(Tags.Count())
                        .And.AllBeOfType<TagSuggestion>();
                }

                [Fact, LogIfTooSlow]
                public async Task SearchesTheName()
                {
                    const string description = "#50";
                    var textFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, 1);

                    var suggestions = await Provider.Query(textFieldInfo);

                    await Database.Tags.Received().GetAll();
                    suggestions.Should().HaveCount(1)
                        .And.AllBeOfType<TagSuggestion>();
                }

                [Fact, LogIfTooSlow]
                public async Task OnlyDisplaysResultsThatHaveAtLeastOneMatchOnEveryWordTyped()
                {
                    const string description = "#5 2";
                    var textFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(description, 1);

                    var suggestions = await Provider.Query(textFieldInfo);

                    await Database.Tags.Received().GetAll();
                    suggestions.Single().Should().BeOfType<TagSuggestion>()
                        .Which.Name.Should().Be("52");
                }
            }

            public sealed class DoesNotQueryTheDatabase : AutocompleteProviderTest
            {
                [Fact, LogIfTooSlow]
                public async Task WhenTheSearchStringIsEmpty()
                {
                    var textFieldInfo = TextFieldInfo.Empty.WithTextAndCursor("", 0);

                    var suggestions = await Provider.Query(textFieldInfo);

                    suggestions.Should().HaveCount(2)
                        .And.AllBeOfType<QuerySymbolSuggestion>();
                }
            }
        }
    }
}