using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace Toggl.Foundation.Tests.Generators
{
    public abstract class ConstructorTestData : IEnumerable<object[]>
    {
        private readonly List<object[]> data;

        protected ConstructorTestData(int parameterCount)
        {
            var iterationCount = (int)Math.Pow(2, parameterCount) - 1;

            data = Enumerable
                .Range(0, iterationCount)
                .Select(outerIndex =>
                    Enumerable
                        .Range(0, parameterCount)
                        .Select(innerIndex => (object)((outerIndex & (1 << innerIndex)) != 0))
                        .ToArray()
                ).ToList();
        }

        public IEnumerator<object[]> GetEnumerator() => data.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed class TwoParameterConstructorTestData : ConstructorTestData
    {
        public TwoParameterConstructorTestData() : base(2) { }
    }

    public sealed class ThreeParameterConstructorTestData : ConstructorTestData
    {
        public ThreeParameterConstructorTestData() : base(3) { }
    }

    public sealed class FourParameterConstructorTestData : ConstructorTestData
    {
        public FourParameterConstructorTestData() : base(4) { }
    }

    public sealed class FiveParameterConstructorTestData : ConstructorTestData
    {
        public FiveParameterConstructorTestData() : base(5) { }
    }

    public sealed class ConstructorTestDataTests
    {
        public sealed class TheGeneratedSequence
        {
            [Theory, LogIfTooSlow]
            [InlineData(typeof(TwoParameterConstructorTestData))]
            [InlineData(typeof(ThreeParameterConstructorTestData))]
            [InlineData(typeof(FourParameterConstructorTestData))]
            [InlineData(typeof(FiveParameterConstructorTestData))]
            public void NeverReturnsASequenceWhereAllElementsAreTrue(Type contructorTestDataType)
            {
                var testData = Activator.CreateInstance(contructorTestDataType) as ConstructorTestData;
                testData.Any(x => x.All(y => (bool)y)).Should().BeFalse();
            }

            [Property]
            public void ReturnsAllPossiblePermutationsForTwoParameters(bool first, bool second)
            {
                if (first && second) return;

                var array = new List<object> { first, second };
                var testData = new TwoParameterConstructorTestData();
                testData.Any(x => x.SequenceEqual(array)).Should().BeTrue();
            }

            [Property]
            public void ReturnsAllPossiblePermutationsForThreeParameters(bool first, bool second, bool third)
            {
                if (first && second && third) return;

                var array = new List<object> { first, second, third };
                var testData = new ThreeParameterConstructorTestData();
                testData.Any(x => x.SequenceEqual(array)).Should().BeTrue();
            }

            [Property]
            public void ReturnsAllPossiblePermutationsForFourParameters(
                bool first, bool second, bool third, bool fourth)
            {
                if (first && second && third && fourth) return;

                var array = new List<object> { first, second, third, fourth };
                var testData = new FourParameterConstructorTestData();
                testData.Any(x => x.SequenceEqual(array)).Should().BeTrue();
            }

            [Property]
            public void ReturnsAllPossiblePermutationsForFiveParameters(
                bool first, bool second, bool third, bool fourth, bool fifth)
            {
                if (first && second && third && fourth && fifth) return;

                var array = new List<object> { first, second, third, fourth, fifth };
                var testData = new FiveParameterConstructorTestData();
                testData.Any(x => x.SequenceEqual(array)).Should().BeTrue();
            }
        }
    }
}
