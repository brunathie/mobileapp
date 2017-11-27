using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class TimeEntryViewModelCollectionTests
    {
        public abstract class TimeEntryViewModelCollectionTest : BaseMvvmCrossTests
        {
            protected DateTime Noon = DateTime.Now.Date.AddHours(12);
            protected ITimeService TimeService { get; } = Substitute.For<ITimeService>();
            protected Subject<DateTimeOffset> TickSubject = new Subject<DateTimeOffset>();
            protected TimeEntryViewModelCollection ViewModel { get; }

            protected TimeEntryViewModelCollectionTest()
            {
                var observable = TickSubject.AsObservable().Publish();
                observable.Connect();
                TimeService.CurrentDateTimeObservable.Returns(observable);

                ViewModel = new TimeEntryViewModelCollection(Noon,
                    Enumerable.Range(0, 10)
                    .Select(i =>
                    {
                        var timeEntry = Substitute.For<IDatabaseTimeEntry>();
                        timeEntry.Duration.Returns((long)TimeSpan.FromHours(1).TotalSeconds);
                        timeEntry.Start.Returns(Noon.AddHours(-i - 1));
                        return timeEntry;
                    }).Select(te => new TimeEntryViewModel(te)).GroupBy(x => x.Start.Date).Single()
                );
            }
        }

        public sealed class TheConstructor : TimeEntryViewModelCollectionTest
        {
            [Fact, LogIfTooSlow]
            public void ThrowsIfTheArgumentIsNull()
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new TimeEntryViewModelCollection(DateTime.Now, null);

                tryingToConstructWithEmptyParameters
                    .ShouldThrow<ArgumentNullException>();
            }

            [Theory, LogIfTooSlow]
            [InlineData(DateTimeKind.Unspecified)]
            [InlineData(DateTimeKind.Utc)]
            public void ThrowsIfDateKindIsNotLocal(DateTimeKind kind)
            {
                var dateTime = new DateTime(2012, 12, 12, 12, 12, 12, kind);

                Action tryingToConstructWithNonLocalDateTime =
                    () => new TimeEntryViewModelCollection(dateTime, Enumerable.Empty<TimeEntryViewModel>());

                tryingToConstructWithNonLocalDateTime
                    .ShouldThrow<ArgumentException>();
            }
        }

        public sealed class TheCollection : TimeEntryViewModelCollectionTest
        {
            [Fact, LogIfTooSlow]
            public void HasTheSameAmountOfItemsAsThePassedGrouping()
            {
                ViewModel.Should().HaveCount(10);
            }
        }

        public sealed class TheTotalTimeProperty : TimeEntryViewModelCollectionTest
        {
            [Fact, LogIfTooSlow]
            public void EqualsTheSumOfTheDurationOfAllTimeEntries()
            {
                ViewModel.TotalTime.Should().Be(TimeSpan.FromHours(10));
            }
        }
    }
}
