using System;
using System.Collections.Generic;
using FluentAssertions;
using Newtonsoft.Json;
using Toggl.Ultrawave.Serialization;
using Xunit;
using JsonSerializer = Toggl.Ultrawave.Serialization.JsonSerializer;

namespace Toggl.Ultrawave.Tests.Serialization
{
    public sealed class ConcreteListTypeConverterTests
    {
        [Fact, LogIfTooSlow]
        public void SerializationMustUseTheConverter()
        {
            var output = serializer.Serialize(SomeItemsContainer);

            output.Should().Be(JsonArray);
        }

        [Fact, LogIfTooSlow]
        public void DeserializationMustUseTheConverter()
        {
            var output = serializer.Deserialize<OtherClass>(JsonArray);

            output.ShouldBeEquivalentTo(SomeItemsContainer, options => options.IncludingProperties());
        }

        [Fact, LogIfTooSlow]
        public void DeserializationFailsWithoutTheConverter()
        {
            Action deserialization = () => serializer.Deserialize<DifferentClass>(JsonArray);

            deserialization.ShouldThrow<JsonSerializationException>();
        }

        private JsonSerializer serializer => new JsonSerializer();

        private interface ISomeInterface
        {
            string SomeName { get; }
        }

        private class SomeClass : ISomeInterface
        {
            public string SomeName { get; set; }
        }

        private class OtherClass
        {
            [JsonConverter(typeof(ConcreteListTypeConverter<SomeClass, ISomeInterface>))]
            public List<ISomeInterface> SomeItems { get; set; }
        }

        private class DifferentClass
        {
            public List<ISomeInterface> SomeItems { get; set; }
        }

        private const string JsonArray = "{\"some_items\":[{\"some_name\":\"A\"},{\"some_name\":\"B\"}]}";

        private static readonly OtherClass SomeItemsContainer = new OtherClass {
            SomeItems = new List<ISomeInterface>
            {
                new SomeClass { SomeName = "A" },
                new SomeClass { SomeName = "B" }
            }
        };
    }
}
