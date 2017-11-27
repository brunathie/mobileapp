using System;
namespace Toggl.Foundation.Login
{
    public interface IGoogleService
    {
        IObservable<string> GetAuthToken();
    }
}
