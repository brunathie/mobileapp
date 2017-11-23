using System;
using MvvmCross.Core.ViewModels;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class OutdatedAppViewModel : MvxViewModel
    {
        private const string togglWebsiteUrl = "https://toggl.com";

        private readonly IBrowserService browserService;

        public IMvxCommand OpenWebsiteCommand { get; }

        public IMvxCommand UpdateAppCommand { get; }

        public OutdatedAppViewModel(IBrowserService browserService)
        {
            Ensure.Argument.IsNotNull(browserService, nameof(browserService));

            this.browserService = browserService;

            UpdateAppCommand = new MvxCommand(updateApp);
            OpenWebsiteCommand = new MvxCommand(openWebsite);
        }

        private void openWebsite()
        {
            browserService.OpenUrl(togglWebsiteUrl);
        }

        private void updateApp()
        {
            browserService.OpenStore();
        }
    }
}
