using System;

namespace Toggl.Foundation.MvvmCross.Services
{
    public interface IBrowserService
    {
        void OpenUrl(string url);

        void OpenStore();
    }
}
