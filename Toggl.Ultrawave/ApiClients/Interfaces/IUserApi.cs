using System;
using Toggl.Multivac.Models;

namespace Toggl.Ultrawave.ApiClients
{
    public interface IUserApi
    {
        IObservable<IUser> Get();
        IObservable<IUser> Update(IUser user);
    }
}
