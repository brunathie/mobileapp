using System;
using System.Globalization;
using FluentAssertions;
using Toggl.Foundation.MvvmCross.Converters;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.Converters
{
    public sealed class TimeSpanToDurationValueConverterTests
    {
        public sealed class TheConvertMethod
        {
            [Fact, LogIfTooSlow]
            public void WorksWithZero()
            {
                var converter = new TimeSpanToDurationValueConverter();

                var result = converter.Convert(TimeSpan.Zero, typeof(string), null, CultureInfo.CurrentCulture);

                result.Should().Be("0:00:00");
            }

            [Fact, LogIfTooSlow]
            public void WorksWithMoreThan24Hours()
            {
                var converter = new TimeSpanToDurationValueConverter();
                var timeSpan = TimeSpan.FromHours(25);

                var result = converter.Convert(timeSpan, typeof(string), null, CultureInfo.CurrentCulture);

                result.Should().Be("25:00:00");
            }

            [Fact, LogIfTooSlow]
            public void WorksForNormalCase()
            {
                var converter = new TimeSpanToDurationValueConverter();
                var timeSpan = TimeSpan.FromSeconds(5 * 60 * 60 + 4 * 60 + 3);

                var result = converter.Convert(timeSpan, typeof(string), null, CultureInfo.CurrentCulture);

                result.Should().Be("5:04:03");
            }
        }
    }
}
