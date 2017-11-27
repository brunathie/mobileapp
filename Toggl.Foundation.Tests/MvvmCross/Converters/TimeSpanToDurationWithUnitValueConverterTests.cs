using System;
using System.Globalization;
using FluentAssertions;
using Toggl.Foundation.MvvmCross.Converters;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.Converters
{
    public sealed class TimeSpanToDurationWithUnitValueConverterTests
    {
        public sealed class TheConvertMethod
        {
            [Fact, LogIfTooSlow]
            public void DoesNotAppendUnitIfTimeSpanIsLongerThanOneHour()
            {
                var converter = new TimeSpanToDurationWithUnitValueConverter();
                var timeSpan = new TimeSpan(12, 32, 42);
                var expected = "12:32:42";

                var actual = converter.Convert(timeSpan, typeof(string), null, CultureInfo.CurrentCulture);

                actual.Should().Be(expected);
            }

            [Fact, LogIfTooSlow]
            public void AppendsTheMinuteUnitIfTimeSpanIsNotLongerThanOneHour()
            {
                var converter = new TimeSpanToDurationWithUnitValueConverter();
                var timeSpan = new TimeSpan(0, 43, 59);
                var expected = $"43:59 {Resources.UnitMin}";

                var actual = converter.Convert(timeSpan, typeof(string), null, CultureInfo.CurrentCulture);

                actual.Should().Be(expected);
            }

            [Fact, LogIfTooSlow]
            public void AppendsTheSecondUnitIfTimeSpanIsLessThanOneMinute()
            {
                var converter = new TimeSpanToDurationWithUnitValueConverter();
                var timeSpan = new TimeSpan(0, 0, 42);
                var expected = $"42 {Resources.UnitSecond}";

                var actual = converter.Convert(timeSpan, typeof(string), null, CultureInfo.CurrentCulture);

                actual.Should().Be(expected);
            }

            [Fact, LogIfTooSlow]
            public void WorksIfMinutesAreZero()
            {
                var converter = new TimeSpanToDurationWithUnitValueConverter();
                var timeSpan = new TimeSpan(12, 0, 12);
                var expected = $"12:00:12";

                var actual = converter.Convert(timeSpan, typeof(string), null, CultureInfo.CurrentCulture);

                actual.Should().Be(expected);
            }

            [Fact, LogIfTooSlow]
            public void DoesNotRemoveLeadingZeroFromMinutes()
            {
                var converter = new TimeSpanToDurationWithUnitValueConverter();
                var timeSpan = new TimeSpan(0, 6, 12);
                var expected = $"06:12 {Resources.UnitMin}";

                var actual = converter.Convert(timeSpan, typeof(string), null, CultureInfo.CurrentCulture);

                actual.Should().Be(expected);
            }

            [Fact, LogIfTooSlow]
            public void DoesNotRemoveLeadingZeroFromHours()
            {
                var converter = new TimeSpanToDurationWithUnitValueConverter();
                var timeSpan = new TimeSpan(3, 6, 12);
                var expected = $"03:06:12";

                var actual = converter.Convert(timeSpan, typeof(string), null, CultureInfo.CurrentCulture);

                actual.Should().Be(expected);
            }

            [Fact, LogIfTooSlow]
            public void WorksForMoreThan24Hours()
            {
                var converter = new TimeSpanToDurationWithUnitValueConverter();
                var timeSpan = new TimeSpan(43, 6, 12);
                var expected = $"43:06:12";

                var actual = converter.Convert(timeSpan, typeof(string), null, CultureInfo.CurrentCulture);

                actual.Should().Be(expected);
            }
        }
    }
}
