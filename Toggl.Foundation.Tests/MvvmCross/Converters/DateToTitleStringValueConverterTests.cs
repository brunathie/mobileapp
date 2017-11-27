using System;
using System.Globalization;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Toggl.Foundation.MvvmCross.Converters;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.Converters
{
    public sealed class DateToTitleStringValueConverterTests
    {
        public sealed class TheConvertMethod
        {
            [Fact, LogIfTooSlow]
            public void ReturnsASpecialCaseStringForTheCurrentDay()
            {
                var converter = new DateToTitleStringValueConverter();
                var date = DateTimeOffset.Now;

                var result = converter.Convert(date, typeof(DateTimeOffset), null, CultureInfo.CurrentCulture);

                result.Should().Be(Resources.Today);
            }

            [Fact, LogIfTooSlow]
            public void ReturnsASpecialCaseStringForThePreviousDay()
            {
                var converter = new DateToTitleStringValueConverter();
                var date = DateTimeOffset.Now.AddDays(-1);

                var result = converter.Convert(date, typeof(DateTimeOffset), null, CultureInfo.CurrentCulture);

                result.Should().Be(Resources.Yesterday);
            }

            [Property]
            public Property ReturnsAFormattedStringForAnyOtherDate()
            {
                var arb = Arb.Default.DateTimeOffset().Filter(d => d < DateTime.UtcNow.AddDays(-1));

                return Prop.ForAll(arb, date =>
                {
                    var converter = new DateToTitleStringValueConverter();

                    var result = converter.Convert(date, typeof(DateTimeOffset), null, CultureInfo.CurrentCulture);

                    result.Should().Be($"{date:ddd, dd MMM}");
                });
            }
        }
    }
}
