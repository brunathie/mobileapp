using System;
using System.Linq;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.Models;
using Toggl.Foundation.Sync.ConflictResolution;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.ConflictResolution
{
    public sealed class TimeEntryRivalsResolverTests
    {
        private readonly TimeEntryRivalsResolver resolver;

        private readonly ITimeService timeService;

        private static readonly DateTimeOffset arbitraryTime = new DateTimeOffset(2017, 9, 1, 12, 0, 0, TimeSpan.Zero);

        private readonly IQueryable<IDatabaseTimeEntry> timeEntries = new EnumerableQuery<IDatabaseTimeEntry>(new[]
        {
            TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 10, Start = arbitraryTime, Duration = (long)TimeSpan.FromHours(2).TotalSeconds }),
            TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 11, Start = arbitraryTime.AddDays(5), Duration = (long)TimeSpan.FromDays(1).TotalSeconds }),
            TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 12, Start = arbitraryTime.AddDays(10), Duration = (long)TimeSpan.FromHours(1).TotalSeconds }),
            TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 13, Start = arbitraryTime.AddDays(15), Duration = (long)TimeSpan.FromSeconds(13).TotalSeconds })
        });

        public TimeEntryRivalsResolverTests()
        {
            timeService = Substitute.For<ITimeService>();
            resolver = new TimeEntryRivalsResolver(timeService);
        }

        [Fact, LogIfTooSlow]
        public void TimeEntryWhichHasDurationSetToNullCanHaveRivals()
        {
            var a = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 1, Duration = null });

            var canHaveRival = resolver.CanHaveRival(a);

            canHaveRival.Should().BeTrue();
        }

        [Property]
        public void TimeEntryWhichHasDurationSetToAnythingElseThanNullCannotHaveRivals(long duration)
        {
            var a = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 1, Duration = duration });

            var canHaveRival = resolver.CanHaveRival(a);

            canHaveRival.Should().BeFalse();
        }

        [Fact, LogIfTooSlow]
        public void TwoTimeEntriesAreRivalsIfBothOfThemHaveTheDurationSetToNull()
        {
            var a = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 1, Duration = null });
            var b = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 2, Duration = null });

            var areRivals = resolver.AreRivals(a).Compile()(b);

            areRivals.Should().BeTrue();
        }

        [Property]
        public void TwoTimeEntriesAreNotRivalsIfTheLatterOneHasTheDurationNotSetToNull(NonNegativeInt b)
        {
            var x = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 1, Duration = null });
            var y = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 2, Duration = b.Get });
            var areRivals = resolver.AreRivals(x).Compile()(y);

            areRivals.Should().BeFalse();
        }

        [Property]
        public void TheTimeEntryWhichHasBeenEditedTheLastWillBeRunningAndTheOtherWillBeStoppedAfterResolution(DateTimeOffset startA, DateTimeOffset startB, DateTimeOffset firstAt, DateTimeOffset secondAt)
        {
            (DateTimeOffset earlier, DateTimeOffset later) =
                firstAt < secondAt ? (firstAt, secondAt) : (secondAt, firstAt);
            var a = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 1, Start = startA, Duration = null, At = earlier });
            var b = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 2, Start = startB, Duration = null, At = later });
            DateTimeOffset now = (startA > startB ? startA : startB).AddHours(5);
            timeService.CurrentDateTime.Returns(now);

            var (fixedEntityA, fixedRivalB) = resolver.FixRivals(a, b, timeEntries);
            var (fixedEntityB, fixedRivalA) = resolver.FixRivals(b, a, timeEntries);

            fixedEntityA.Duration.Should().NotBeNull();
            fixedRivalA.Duration.Should().NotBeNull();
            fixedRivalB.Duration.Should().BeNull();
            fixedEntityB.Duration.Should().BeNull();
        }

        [Fact, LogIfTooSlow]
        public void TheStoppedTimeEntryMustBeMarkedAsSyncNeededAndTheStatusOfTheOtherOneShouldNotChange()
        {
            var a = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 1, Duration = null, At = arbitraryTime.AddDays(10) });
            var b = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 2, Duration = null, At = arbitraryTime.AddDays(11) });

            var (fixedA, fixedB) = resolver.FixRivals(a, b, timeEntries);

            fixedA.SyncStatus.Should().Be(SyncStatus.SyncNeeded);
            fixedB.SyncStatus.Should().Be(SyncStatus.InSync);
        }

        [Fact, LogIfTooSlow]
        public void TheStoppedEntityMustHaveTheStopTimeEqualToTheStartTimeOfTheNextEntryInTheDatabase()
        {
            var a = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 1, Duration = null, At = arbitraryTime.AddDays(10), Start = arbitraryTime.AddDays(12) });
            var b = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 2, Duration = null, At = arbitraryTime.AddDays(11), Start = arbitraryTime.AddDays(13) });

            var (fixedA, _) = resolver.FixRivals(a, b, timeEntries);

            fixedA.Duration.Should().Be((long)TimeSpan.FromDays(3).TotalSeconds);
        }

        [Fact, LogIfTooSlow]
        public void TheStoppedEntityMustHaveTheStopTimeEqualToTheCurrentDateTimeOfTheTimeServiceWhenThereIsNoNextEntryInTheDatabase()
        {
            var now = arbitraryTime.AddDays(25);
            timeService.CurrentDateTime.Returns(now);
            var a = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 1, Duration = null, At = arbitraryTime.AddDays(21), Start = arbitraryTime.AddDays(20) });
            var b = TimeEntry.Clean(new Ultrawave.Models.TimeEntry { Id = 2, Duration = null, At = arbitraryTime.AddDays(22), Start = arbitraryTime.AddDays(21) });

            var (fixedA, _) = resolver.FixRivals(a, b, timeEntries);

            fixedA.Duration.Should().Be((long)TimeSpan.FromDays(5).TotalSeconds);
        }
    }
}
