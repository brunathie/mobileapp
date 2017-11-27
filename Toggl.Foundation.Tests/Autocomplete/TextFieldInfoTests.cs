using System.Linq;
using FluentAssertions;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.PrimeRadiant.Models;
using Xunit;
using static Toggl.Multivac.Extensions.FunctionalExtensions;

namespace Toggl.Foundation.Tests.Autocomplete
{
    public sealed class TextFieldInfoTests
    {
        public abstract class TextFieldInfoTest
        {
            protected const long WorkspaceId = 9;
            protected const long ProjectId = 10;
            protected const string ProjectName = "Toggl";
            protected const string ProjectColor = "#F41F19";
            protected const string Description = "Testing Toggl mobile apps";
            protected const long TaskId = 13;
            protected const string TaskName = "Test Toggl apps";

            protected TextFieldInfo CreateDefaultTextFieldInfo() => TextFieldInfo.Empty
                .WithTextAndCursor(Description, Description.Length)
                .WithProjectAndTaskInfo(WorkspaceId, ProjectId, ProjectName, ProjectColor, TaskId, TaskName);
        }

        public sealed class TheDescriptionCursorPositionProperty
        {
            [Property]
            public void AlwaysReturnsACursorPositionThatsLessThanOrEqualTheLengthOfTheText(string text, int cursor)
            {
                if (text == null) return;

                var textFieldInfo = TextFieldInfo.Empty.WithTextAndCursor(text, cursor);

                textFieldInfo.DescriptionCursorPosition.Should().BeLessOrEqualTo(text.Length);
            }
        }

        public sealed class TheWithWorkspaceMethod : TextFieldInfoTest
        {
            [Fact, LogIfTooSlow]
            public void ReturnsTheSameObjectWhenWorkspaceDoesNotChange()
            {
                var textFieldInfo = CreateDefaultTextFieldInfo();

                var changedFieldInfo = textFieldInfo.WithWorkspace(WorkspaceId);

                changedFieldInfo.Should().Be(textFieldInfo);
            }

            [Fact, LogIfTooSlow]
            public void RemovesProjectTaskAndTagsWhenWorkspaceChanges()
            {
                var tag = createTagSuggestion(123);
                var textFieldInfo = CreateDefaultTextFieldInfo().AddTag(tag);

                var changedFieldInfo = textFieldInfo.WithWorkspace(99);

                changedFieldInfo.Should().NotBe(textFieldInfo);
                changedFieldInfo.ProjectId.Should().BeNull();
                changedFieldInfo.ProjectName.Should().BeNullOrEmpty();
                changedFieldInfo.ProjectColor.Should().BeNullOrEmpty();
                changedFieldInfo.TaskId.Should().BeNull();
                changedFieldInfo.TaskName.Should().BeNullOrEmpty();
                changedFieldInfo.Tags.Should().BeEmpty();
            }
        }

        public sealed class TheWithTextAndCursorMethod : TextFieldInfoTest
        {
            [Fact, LogIfTooSlow]
            public void ChangesOnlyTheTextAndCursorPositionWhileMaintainingTheOtherFields()
            {
                const string newDescription = "Some other text";
                var expected = TextFieldInfo.Empty
                    .WithTextAndCursor(newDescription, newDescription.Length)
                    .WithProjectInfo(WorkspaceId, ProjectId, ProjectName, ProjectColor);

                var textFieldInfo =
                    CreateDefaultTextFieldInfo()
                        .WithTextAndCursor(newDescription, newDescription.Length);

                textFieldInfo.Should().Be(expected);
            }
        }

        public sealed class TheWithProjectInfoMethod : TextFieldInfoTest
        {
            [Fact, LogIfTooSlow]
            public void ChangesOnlyTheProjectRelatedInfoWhileMaintainingTheOtherFields()
            {
                const long newProjectId = 11;
                const string newProjectName = "Some other project";
                const string newProjectColor = "Some other project";
                var expected = TextFieldInfo.Empty
                    .WithTextAndCursor(Description, Description.Length)
                    .WithProjectInfo(WorkspaceId, newProjectId, newProjectName, newProjectColor);

                var textFieldInfo =
                    CreateDefaultTextFieldInfo()
                        .WithProjectInfo(WorkspaceId, newProjectId, newProjectName, newProjectColor);

                textFieldInfo.Should().Be(expected);
            }

            [Fact, LogIfTooSlow]
            public void RemovesTheTaskIdAndTaskName()
            {
                const long newProjectId = 11;
                const string newProjectName = "Some other project";
                const string newProjectColor = "Some other project";

                var textFieldInfo =
                    CreateDefaultTextFieldInfo()
                        .WithProjectInfo(WorkspaceId, newProjectId, newProjectName, newProjectColor);

                textFieldInfo.TaskId.Should().BeNull();
                textFieldInfo.TaskName.Should().BeEmpty();
            }
        }

        public sealed class TheWithProjectAndTaskInfoMethod : TextFieldInfoTest
        {
            private const long workspaceId = 10;
            private const long projectId = 20;
            private const string projectName = "New project";
            private const string projectColor = "FFAABB";
            private const long taskId = 30;
            private const string taskName = "New task";

            [Fact, LogIfTooSlow]
            public void SetsTheProjectInfo()
            {
                var textFieldInfo = CreateDefaultTextFieldInfo()
                    .WithProjectAndTaskInfo(workspaceId, projectId, projectName, projectColor, taskId, taskName);

                textFieldInfo.ProjectId.Should().Be(projectId);
                textFieldInfo.ProjectName.Should().Be(projectName);
                textFieldInfo.ProjectColor.Should().Be(projectColor);
            }

            [Fact, LogIfTooSlow]
            public void SetsTheTaskInfo()
            {
                var textFieldInfo = CreateDefaultTextFieldInfo()
                    .WithProjectAndTaskInfo(workspaceId, projectId, projectName, projectColor, taskId, taskName);

                textFieldInfo.TaskId.Should().Be(taskId);
                textFieldInfo.TaskName.Should().Be(taskName);
            }
        }

        public sealed class TheRemoveProjectQueryFromDescriptionIfNeededMethod : TextFieldInfoTest
        {
            [Fact, LogIfTooSlow]
            public void RemovesTheProjectQueryIfAnyAtSymbolIsPresent()
            {
                var newDescription = $"{Description}@something";

                var textFieldInfo = TextFieldInfo.Empty
                    .WithTextAndCursor(newDescription, newDescription.Length)
                    .WithProjectInfo(WorkspaceId, ProjectId, ProjectName, ProjectColor)
                    .RemoveProjectQueryFromDescriptionIfNeeded();

                textFieldInfo.Text.Should().Be(Description);
            }

            [Fact, LogIfTooSlow]
            public void DoesNotChangeAnyPropertyIfThereIsNoProjectQueryInTheDescription()
            {
                var textFieldInfo = CreateDefaultTextFieldInfo();

                var newTextFieldInfo =
                    textFieldInfo.RemoveProjectQueryFromDescriptionIfNeeded();

                textFieldInfo.Should().Be(newTextFieldInfo);
            }
        }

        public sealed class RemoveTagQueryFromDescriptionIfNeeded : TextFieldInfoTest
        {
            [Fact, LogIfTooSlow]
            public void RemovesTheProjectQueryIfAnyHashtagSymbolIsPresent()
            {
                var newDescription = $"{Description}#something";

                var textFieldInfo = TextFieldInfo.Empty
                    .WithTextAndCursor(newDescription, newDescription.Length)
                    .RemoveTagQueryFromDescriptionIfNeeded();

                textFieldInfo.Text.Should().Be(Description);
            }

            [Fact, LogIfTooSlow]
            public void DoesNotChangeAnyPropertyIfThereIsNoProjectQueryInTheDescription()
            {
                var textFieldInfo = CreateDefaultTextFieldInfo();

                var newTextFieldInfo =
                    textFieldInfo.RemoveTagQueryFromDescriptionIfNeeded();

                textFieldInfo.Should().Be(newTextFieldInfo);
            }
        }

        public sealed class TheRemoveProjectInfoMethod : TextFieldInfoTest
        {
            [Fact, LogIfTooSlow]
            public void RemovesAllProjectRelatedFields()
            {
                var newDescription = $"{Description}@something";

                var textFieldInfo = TextFieldInfo.Empty
                    .WithTextAndCursor(newDescription, newDescription.Length)
                    .WithProjectInfo(WorkspaceId, ProjectId, ProjectName, ProjectColor)
                    .RemoveProjectInfo();

                textFieldInfo.ProjectId.Should().BeNull();
                textFieldInfo.ProjectName.Should().BeEmpty();
                textFieldInfo.ProjectColor.Should().BeEmpty();
            }
        }

        public sealed class TheClearTagsMethod
        {
            [Fact, LogIfTooSlow]
            public void RemovesAllTags()
            {
                var tags = Enumerable.Range(10, 10)
                    .Select(createTagSuggestion);
                var textFieldInfo = TextFieldInfo.Empty;
                foreach (var tag in tags)
                    textFieldInfo = textFieldInfo.AddTag(tag);

                var newtextFieldInfo = textFieldInfo.ClearTags();

                newtextFieldInfo.Tags.Should().BeEmpty();
            }
        }

        private static TagSuggestion createTagSuggestion(int id)
        {
            var tag = Substitute.For<IDatabaseTag>();
            tag.Id.Returns(id);
            tag.Name.Returns($"Tag{id}");
            return new TagSuggestion(tag);
        }
    }
}
