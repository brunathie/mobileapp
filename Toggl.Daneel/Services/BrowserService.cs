using System;
using Foundation;
using Toggl.Foundation.MvvmCross.Services;
using UIKit;

namespace Toggl.Daneel.Services
{
    public sealed class BrowserService : IBrowserService
    {
        public void OpenStore()
        {
            OpenUrl("");
        }

        public void OpenUrl(string url)
        {
            var nsUrl = new NSUrl(url);
            UIApplication.SharedApplication.OpenUrl(nsUrl);
        }
    }
}
