﻿using System;
using Toggl.Multivac;
using Toggl.Multivac.Models;

namespace Toggl.Ultrawave.ApiClients
{
    public interface IUserApi
    {
        IObservable<IUser> Get();
        IObservable<string> ResetPassword(Email email);
        IObservable<IUser> SignUp(Email email, string password);
        IObservable<IUser> Update(IUser user);
    }
}
