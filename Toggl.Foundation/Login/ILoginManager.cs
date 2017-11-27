using System;
using Toggl.Foundation.DataSources;
using Toggl.Multivac;

namespace Toggl.Foundation.Login
{
    public interface ILoginManager
    {
        ITogglDataSource GetDataSourceIfLoggedIn();

        IObservable<ITogglDataSource> Login(Email email, string password);
        IObservable<ITogglDataSource> SignUp(Email email, string password);
        IObservable<ITogglDataSource> RefreshToken(string password);

        IObservable<string> ResetPassword(Email email);
    }
}
