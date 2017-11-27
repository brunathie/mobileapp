using FluentAssertions;
using Toggl.Ultrawave.Serialization;
using Xunit;

namespace Toggl.Ultrawave.Tests.Serialization
{
    public sealed class JsonSerializerTests
    {
        private class TestModel
        {
            public string FooBar { get; set; }

            [IgnoreWhenPosting]
            public string IgnoredWhenPosting { get; set; }
        }

        public sealed class TheSerializeMethod
        {
            [Fact, LogIfTooSlow]
            public void CreatesSnakeCasedJson()
            {
                var testObject = new TestModel { FooBar = "Foo", IgnoredWhenPosting = "Baz" };
                const string expectedJson = "{\"foo_bar\":\"Foo\",\"ignored_when_posting\":\"Baz\"}";

                var jsonSerializer = new JsonSerializer();
                var actual = jsonSerializer.Serialize(testObject);

                actual.Should().Be(expectedJson);
            }

            [Fact, LogIfTooSlow]
            public void IgnoresPropertiesWithTheIgnoreWhenPostingAttribute()
            {
                var testObject = new TestModel { FooBar = "Foo", IgnoredWhenPosting = "Baz" };
                const string expectedJson = "{\"foo_bar\":\"Foo\"}";

                var jsonSerializer = new JsonSerializer();
                var actual = jsonSerializer.Serialize(testObject, SerializationReason.Post);

                actual.Should().Be(expectedJson);
            }
        }

        public sealed class TheDeserializeMethod
        {
            [Fact, LogIfTooSlow]
            public void ExpectsSnakeCasedJson()
            {
                const string testJson = "{\"foo_bar\":\"Foo\",\"ignored_when_posting\":\"Baz\"}";
                var expectedObject = new TestModel { FooBar = "Foo", IgnoredWhenPosting = "Baz" };

                var jsonSerializer = new JsonSerializer();
                var actual = jsonSerializer.Deserialize<TestModel>(testJson);

                actual.FooBar.Should().Be(expectedObject.FooBar);
            }
        }
    }
}
