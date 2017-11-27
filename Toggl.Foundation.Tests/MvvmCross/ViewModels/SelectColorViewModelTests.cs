using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MvvmCross.Platform.UI;
using NSubstitute;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SelectColorViewModelTests
    {
        public abstract class SelectColorViewModelTest : BaseViewModelTests<SelectColorViewModel>
        {
            protected override SelectColorViewModel CreateViewModel()
                => new SelectColorViewModel(NavigationService);
        }

        public sealed class TheConstructor
        {
            [Fact, LogIfTooSlow]
            public void ThrowsIfTheArgumentIsNull()
            {
                Action tryingToConstructWithEmptyParameter =
                    () => new SelectColorViewModel(null);

                tryingToConstructWithEmptyParameter.ShouldThrow<ArgumentNullException>();
            }
        }

        public sealed class TheSelectColorCommandCommand : SelectColorViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void ChangesTheSelectedColor()
            {
                var initiallySelectedColor = Color.DefaultProjectColors.First();
                var parameters = ColorParameters.Create(initiallySelectedColor, true);
                ViewModel.Prepare(parameters);
                var colorToSelect = ViewModel.SelectableColors.Last();

                ViewModel.SelectColorCommand.Execute(colorToSelect);

                ViewModel.SelectableColors.Single(c => c.Selected).Color.ARGB.Should().Be(colorToSelect.Color.ARGB);
            }
        }

        public sealed class ThePrepareCommand : SelectColorViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void AddsFourteenItemsToTheListOfSelectableColorsIfTheUserIsNotPro()
            {
                var parameters = ColorParameters.Create(MvxColors.Azure, false);
                ViewModel.Prepare(parameters);

                ViewModel.SelectableColors.Should().HaveCount(14);
            }

            [Fact, LogIfTooSlow]
            public void AddsFifteenItemsToTheListOfSelectableColorsIfTheUserIsPro()
            {
                var parameters = ColorParameters.Create(MvxColors.Azure, true);
                ViewModel.Prepare(parameters);

                ViewModel.SelectableColors.Should().HaveCount(15);
            }

            [Fact, LogIfTooSlow]
            public void SelectsTheColorPassedAsTheParameter()
            {
                var passedColor = Color.DefaultProjectColors.Skip(3).First();
                var parameters = ColorParameters.Create(passedColor, false);

                ViewModel.Prepare(parameters);

                ViewModel.SelectableColors.Single(c => c.Selected).Color.ARGB.Should().Be(passedColor.ARGB);
            }

            [Fact, LogIfTooSlow]
            public void SelectsTheFirstColorIfThePassedColorIsNotPartOfTheDefaultColorsAndWorkspaceIsNotPro()
            {
                var expected = Color.DefaultProjectColors.First();

                var parameters = ColorParameters.Create(MvxColors.Azure, false);
                ViewModel.Prepare(parameters);

                ViewModel.SelectableColors.Single(c => c.Selected).Color.ARGB.Should().Be(expected.ARGB);
            }

            [Fact, LogIfTooSlow]
            public void SelectsThePassedColorIfThePassedColorIsNotPartOfTheDefaultColorsAndWorkspaceIsPro()
            {
                var parameters = ColorParameters.Create(MvxColors.Azure, true);
                ViewModel.Prepare(parameters);

                ViewModel.SelectableColors.Single(c => c.Selected).Color.ARGB.Should().Be(MvxColors.Azure.ARGB);
            }
        }

        public class TheCloseCommand : SelectColorViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                await ViewModel.CloseCommand.ExecuteAsync();

                await NavigationService.Received().Close(Arg.Is(ViewModel), Arg.Any<MvxColor>());
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTheDefaultParameter()
            {
                var color = Color.DefaultProjectColors.Last();
                var parameters = ColorParameters.Create(color, true);
                ViewModel.Prepare(parameters);

                await ViewModel.CloseCommand.ExecuteAsync();

                NavigationService.Received().Close(Arg.Is(ViewModel), Arg.Is(color)).Wait();
            }
        }

        public sealed class TheSaveCommand : SelectColorViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                await ViewModel.CloseCommand.ExecuteAsync();

                await NavigationService.Received().Close(Arg.Is(ViewModel), Arg.Any<MvxColor>());
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTheSelectedColor()
            {
                var parameters = ColorParameters.Create(MvxColors.Azure, true);
                ViewModel.Prepare(parameters);
                var expected = ViewModel.SelectableColors.First();
                ViewModel.SelectColorCommand.Execute(ViewModel.SelectableColors.First());

                await ViewModel.SaveCommand.ExecuteAsync();

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Is<MvxColor>(c => c.ARGB == expected.Color.ARGB));
            }
        }
    }
}
