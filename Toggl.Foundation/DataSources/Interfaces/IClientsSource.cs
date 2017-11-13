using System;
using System.Collections.Generic;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.DataSources
{
    public interface IClientsSource
    {
        IObservable<IDatabaseClient> GetById(long id);

        IObservable<IDatabaseClient> Create(string name, long workspaceId);

        IObservable<IEnumerable<IDatabaseClient>> GetAllInWorkspace(long workspaceId);
    }
}