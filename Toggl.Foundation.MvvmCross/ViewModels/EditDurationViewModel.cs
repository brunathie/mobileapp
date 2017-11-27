using System;
using System.Threading.Tasks;
using MvvmCross.Core.Navigation;
using MvvmCross.Core.ViewModels;
using PropertyChanged;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class EditDurationViewModel : MvxViewModel<DurationParameter, DurationParameter>
    {
        private readonly ITimeService timeService;
        private readonly IMvxNavigationService navigationService;

        private bool isRunning => defaultResult?.Duration == null;
        private DurationParameter defaultResult;
        private DurationParameter result
            => DurationParameter.WithStartAndDuration(StartTime, isRunning ? (TimeSpan?)null : Duration);

        public DateTimeOffset StartTime { get; private set; }

        [DependsOn(nameof(Duration), nameof(StartTime))]
        public DateTimeOffset StopTime => StartTime + Duration;

        private TimeSpan duration;

        public TimeSpan Duration
        {
            get => duration;
            set => onDurationChanged(value);
        }

        public IMvxAsyncCommand SaveCommand { get; }

        public IMvxAsyncCommand CloseCommand { get; }

        public EditDurationViewModel(IMvxNavigationService navigationService, ITimeService timeService)
        {
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));

            this.timeService = timeService;
            this.navigationService = navigationService;

            SaveCommand = new MvxAsyncCommand(save);
            CloseCommand = new MvxAsyncCommand(close);
        }

        public override void Prepare(DurationParameter parameter)
        {
            defaultResult = parameter;

            if (isRunning)
            {
                timeService.CurrentDateTimeObservable
                           .Subscribe(currentTime => Duration = currentTime - StartTime);
            }

            StartTime = parameter.Start;
            Duration = parameter.Duration ?? timeService.CurrentDateTime - StartTime;
        }

        private Task close()
            => navigationService.Close(this, defaultResult);

        private Task save()
            => navigationService.Close(this, result);

        private void onDurationChanged(TimeSpan changedDuration)
        {
            if (isRunning)
                StartTime = timeService.CurrentDateTime - changedDuration;

            duration = changedDuration;
        }
    }
}
