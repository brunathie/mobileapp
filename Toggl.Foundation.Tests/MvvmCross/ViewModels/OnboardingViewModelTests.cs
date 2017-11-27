using System;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Login;
using Toggl.Foundation.MvvmCross.ViewModels;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class OnboardingViewModelTests
    {
        public abstract class OnboardingViewModelTest : BaseViewModelTests<OnboardingViewModel>
        {
            protected override OnboardingViewModel CreateViewModel()
                => new OnboardingViewModel(NavigationService);
        }

        public sealed class TheConstructor
        {
            [Fact, LogIfTooSlow]
            public void ThrowsIfTheArgumentsIsNull()
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new OnboardingViewModel(null);

                tryingToConstructWithEmptyParameters
                    .ShouldThrow<ArgumentNullException>();
            }
        }

        public sealed class TheCurrentPageProperty : OnboardingViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void DoesNotAllowUsersToSetItsValueToAValueGreaterThanTheNumberOfPagesMinusOne()
            {
                ViewModel.CurrentPage = OnboardingViewModel.TrackPage;

                ViewModel.CurrentPage = ViewModel.NumberOfPages;
                ViewModel.CurrentPage = ViewModel.NumberOfPages + 1;

                ViewModel.CurrentPage.Should().Be(OnboardingViewModel.TrackPage);
            }

            [Fact, LogIfTooSlow]
            public void DoesNotAllowUsersToSetItsValueToANegativeNumber()
            {
                ViewModel.CurrentPage = OnboardingViewModel.TrackPage;

                ViewModel.CurrentPage = -1;

                ViewModel.CurrentPage.Should().Be(OnboardingViewModel.TrackPage);
            }
        }

        public sealed class TheIsFirstPageProperty : OnboardingViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(OnboardingViewModel.TrackPage)]
            [InlineData(OnboardingViewModel.LogPage)]
            [InlineData(OnboardingViewModel.SummaryPage)]
            [InlineData(OnboardingViewModel.LoginPage)]
            public void OnlyReturnsTrueForTheFirstPage(int page)
            {
                ViewModel.CurrentPage = page;

                var expected = page == 0;
                ViewModel.IsFirstPage.Should().Be(expected);
            }
        }

        public sealed class TheIsLastPageProperty : OnboardingViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(OnboardingViewModel.TrackPage)]
            [InlineData(OnboardingViewModel.LogPage)]
            [InlineData(OnboardingViewModel.SummaryPage)]
            [InlineData(OnboardingViewModel.LoginPage)]
            public void OnlyReturnsTrueInThePageWhoseIndexEqualsToTheNumberOfPagesMinusOne(int page)
            {
                ViewModel.CurrentPage = page;

                var expected = page == ViewModel.NumberOfPages - 1;
                ViewModel.IsLastPage.Should().Be(expected);
            }
        }

        public sealed class TheSkipCommand : OnboardingViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void GoesStraightToTheLastPage()
            {
                ViewModel.SkipCommand.Execute();

                ViewModel.CurrentPage.Should().Be(OnboardingViewModel.LoginPage);
            }
        }

        public sealed class TheNextCommand : OnboardingViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void AdvancesToTheNextPage()
            {
                ViewModel.CurrentPage = OnboardingViewModel.TrackPage;

                ViewModel.NextCommand.Execute();

                ViewModel.CurrentPage.Should().Be(OnboardingViewModel.LogPage);
            }

            [Fact, LogIfTooSlow]
            public void CannotBeExecutedWhenOnTheLastPage()
            {
                ViewModel.CurrentPage = OnboardingViewModel.LoginPage;

                ViewModel.NextCommand.CanExecute().Should().BeFalse();
            }
        }

        public sealed class ThePreviousCommand : OnboardingViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void ReturnsToThePreviousPage()
            {
                ViewModel.CurrentPage = OnboardingViewModel.LogPage;

                ViewModel.PreviousCommand.Execute();

                ViewModel.CurrentPage.Should().Be(OnboardingViewModel.TrackPage);
            }

            [Fact, LogIfTooSlow]
            public void CannotBeExecutedWhenOnTheFirstPage()
            {
                ViewModel.CurrentPage = OnboardingViewModel.TrackPage;

                ViewModel.PreviousCommand.CanExecute().Should().BeFalse();
            }
        }

        public sealed class TheLoginCommand : OnboardingViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task RequestsTheLoginViewModelFromTheNavigationService()
            {
                await ViewModel.LoginCommand.ExecuteAsync();

                await NavigationService.Received().Navigate(typeof(LoginViewModel), Arg.Any<LoginType>());
            }
        }
    }
}
