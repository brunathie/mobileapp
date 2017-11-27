using System;
using FluentAssertions;
using Toggl.Ultrawave.Models;
using Xunit;

namespace Toggl.Ultrawave.Tests.Models
{
    public sealed class TagTests
    {
        private string validJson
            => "{\"id\":2024667,\"workspace_id\":424213,\"name\":\"mobile\",\"at\":\"2014-04-25T10:10:13+00:00\",\"deleted_at\":\"2014-04-25T10:10:10+00:00\"}";

        private Tag validTag => new Tag
        {
            Id = 2024667,
            WorkspaceId = 424213,
            Name = "mobile",
            At = new DateTimeOffset(2014, 04, 25, 10, 10, 13, TimeSpan.Zero),
            DeletedAt = new DateTimeOffset(2014, 04, 25, 10, 10, 10, TimeSpan.Zero)
        };

        [Fact, LogIfTooSlow]
        public void HasConstructorWhichCopiesValuesFromInterfaceToTheNewInstance()
        {
            var clonedObject = new Tag(validTag);

            clonedObject.Should().NotBeSameAs(validTag);
            clonedObject.ShouldBeEquivalentTo(validTag, options => options.IncludingProperties());
        }

        [Fact, LogIfTooSlow]
        public void CanBeDeserialized()
        {
            SerializationHelper.CanBeDeserialized(validJson, validTag);
        }

        [Fact, LogIfTooSlow]
        public void CanBeSerialized()
        {
            SerializationHelper.CanBeSerialized(validJson, validTag);
        }
    }
}
