using System;
using System.Collections.Generic;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.DataSources
{
    public interface ITagsSource
    {
        IObservable<IDatabaseTag> GetById(long id);
        IObservable<IEnumerable<IDatabaseTag>> GetAll();
        IObservable<IDatabaseTag> Create(string name, long workspaceId);
        IObservable<IEnumerable<IDatabaseTag>> GetAll(Func<IDatabaseTag, bool> predicate);
    }
}