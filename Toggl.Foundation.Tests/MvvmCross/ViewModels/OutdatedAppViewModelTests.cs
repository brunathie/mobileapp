using System;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class OutdatedAppViewModelTests
    {
        public abstract class OutdatedAppViewModelTest : BaseViewModelTests<OutdatedAppViewModel>
        {
            protected IBrowserService BrowserService { get; } = Substitute.For<IBrowserService>();

            protected override OutdatedAppViewModel CreateViewModel()
                => new OutdatedAppViewModel(BrowserService);
        }

        public sealed class TheConstructor : OutdatedAppViewModelTest
        {
            [Fact]
            public void ThrowsIfTheArgumentIsNull()
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new OutdatedAppViewModel(null);

                tryingToConstructWithEmptyParameters
                    .ShouldThrow<ArgumentNullException>();
            }
        }

        public sealed class TheUpdateAppCommand : OutdatedAppViewModelTest
        {
            [Fact]
            public void CallsTheOpenStoreMethodOfTheBrowserService()
            {
                ViewModel.UpdateAppCommand.Execute();

                BrowserService.Received().OpenStore();
            }
        }

        public sealed class TheOpenWebsiteCommand : OutdatedAppViewModelTest
        {
            [Fact]
            public void TogglesTheCurrentValueOfTheToggleUseTwentyFourHourClockProperty()
            {
                const string togglWebsiteUrl = "https://toggl.com";

                ViewModel.OpenWebsiteCommand.Execute();

                BrowserService.Received().OpenUrl(Arg.Is(togglWebsiteUrl));
            }
        }
    }
}
