using System;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public class SelectDateTimeViewModelTests
    {
        public class SelectDateTimeDialogViewModelTest : BaseViewModelTests<SelectDateTimeViewModel>
        {
            protected override SelectDateTimeViewModel CreateViewModel()
                => new SelectDateTimeViewModel(NavigationService);

            protected DatePickerParameters GenerateParameterForTime(DateTimeOffset now)
                => DatePickerParameters.WithDates(now, now.AddHours(-1), now.AddHours(+1));
        }

        public class TheConstructor
        {
            [Fact, LogIfTooSlow]
            public void ThrowsIfTheArgumentIsNull()
            {
                Action tryingToConstructWithEmptyParameter
                    = () => new SelectDateTimeViewModel(null);

                tryingToConstructWithEmptyParameter.ShouldThrow<ArgumentNullException>();
            }
        }

        public class TheCurrentDateTimeCommand : SelectDateTimeDialogViewModelTest
        {
            [Property]
            public void DoesNotAcceptValuesGreaterThanTheMaxValue(DateTimeOffset now)
            {
                if (DateTimeOffset.MinValue.AddHours(1) <= now ||
                    DateTimeOffset.MaxValue.AddHours(-1) >= now) return;

                var parameter = GenerateParameterForTime(now);
                ViewModel.Prepare(parameter);

                ViewModel.CurrentDateTime = parameter.MaxDate.AddMinutes(3);

                ViewModel.CurrentDateTime.Should().Be(parameter.MaxDate);
            }

            [Property]
            public void DoesNotAcceptValuesSmallerThanTheMinValue(DateTimeOffset now)
            {
                if (DateTimeOffset.MinValue.AddHours(1) <= now ||
                    DateTimeOffset.MaxValue.AddHours(-1) >= now) return;

                var parameter = GenerateParameterForTime(now);
                ViewModel.Prepare(parameter);

                ViewModel.CurrentDateTime = parameter.MinDate.AddMinutes(-3);

                ViewModel.CurrentDateTime.Should().Be(parameter.MinDate);
            }
        }

        public class TheCloseCommand : SelectDateTimeDialogViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                await ViewModel.CloseCommand.ExecuteAsync();

                await NavigationService.Received().Close(Arg.Is(ViewModel), Arg.Any<DateTimeOffset>());
            }

            [Property]
            public void ReturnsTheDefaultParameter(DateTimeOffset now)
            {
                if (DateTimeOffset.MinValue.AddHours(1) <= now ||
                    DateTimeOffset.MaxValue.AddHours(-1) >= now) return;

                var parameter = GenerateParameterForTime(now);
                ViewModel.Prepare(parameter);

                ViewModel.CloseCommand.ExecuteAsync().Wait();

                NavigationService.Received().Close(Arg.Is(ViewModel), Arg.Is(now)).Wait();
            }
        }

        public class TheSaveCommand : SelectDateTimeDialogViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                await ViewModel.CloseCommand.ExecuteAsync();

                await NavigationService.Received().Close(Arg.Is(ViewModel), Arg.Any<DateTimeOffset>());
            }

            [Property]
            public void ReturnsAValueThatReflectsTheChangesToDuration(DateTimeOffset now, DateTimeOffset dateTimeOffset)
            {
                if (DateTimeOffset.MinValue.AddHours(1) <= dateTimeOffset ||
                    DateTimeOffset.MaxValue.AddHours(-1) >= dateTimeOffset) return;

                var parameter = GenerateParameterForTime(dateTimeOffset);
                parameter.CurrentDate = now;
                ViewModel.Prepare(parameter);
                ViewModel.CurrentDateTime = dateTimeOffset;

                ViewModel.SaveCommand.ExecuteAsync().Wait();
                
                NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Is<DateTimeOffset>(p => p == dateTimeOffset))
                    .Wait();
            }
        }
    }
}
