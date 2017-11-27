using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class TimeEntryViewModelTests
    {
        public abstract class TimeEntryViewModelTest : BaseMvvmCrossTests
        {
            protected IDatabaseProject Project = Substitute.For<IDatabaseProject>();
            protected ITimeService TimeService { get; } = Substitute.For<ITimeService>();
            protected IDatabaseTimeEntry MockTimeEntry = Substitute.For<IDatabaseTimeEntry>();

            protected Subject<DateTimeOffset> TickSubject = new Subject<DateTimeOffset>();

            protected TimeEntryViewModelTest()
            {
                var observable = TickSubject.AsObservable().Publish();
                observable.Connect();
                TimeService.CurrentDateTimeObservable.Returns(observable);
            }
        }

        public sealed class TheConstructor : TimeEntryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void ThrowsIfTheArgumentIsNull()
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new TimeEntryViewModel(null);

                tryingToConstructWithEmptyParameters
                    .ShouldThrow<ArgumentNullException>();
            }
        }

        public sealed class TheHasProjectProperty : TimeEntryViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public void ChecksIfTheTimeEntryProvidedHasANonNullProject(bool hasProject)
            {
                MockTimeEntry.Duration.Returns((long)TimeSpan.FromHours(1).TotalSeconds);
                MockTimeEntry.Project.Returns(hasProject ? Project : null);

                var viewModel = new TimeEntryViewModel(MockTimeEntry);

                viewModel.HasProject.Should().Be(hasProject);
            }
        }
    }
}
