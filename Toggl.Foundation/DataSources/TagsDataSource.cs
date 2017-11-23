using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Toggl.Foundation.Models;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.DataSources
{
    public sealed class TagsDataSource : ITagsSource
    {
        private readonly IIdProvider idProvider;
        private readonly ITimeService timeService;
        private readonly IRepository<IDatabaseTag> repository;

        public TagsDataSource(IIdProvider idProvider, IRepository<IDatabaseTag> repository, ITimeService timeService)
        {
            Ensure.Argument.IsNotNull(idProvider, nameof(idProvider));
            Ensure.Argument.IsNotNull(repository, nameof(repository));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));

            this.idProvider = idProvider;
            this.repository = repository;
            this.timeService = timeService;
        }

        public IObservable<IEnumerable<IDatabaseTag>> GetAll()
            => repository.GetAll();

        public IObservable<IEnumerable<IDatabaseTag>> GetAll(Func<IDatabaseTag, bool> predicate)
            => repository.GetAll(predicate);

        public IObservable<IDatabaseTag> GetById(long id)
            => repository.GetById(id);

        public IObservable<IDatabaseTag> Create(string name, long workspaceId)
            => idProvider
                .GetNextIdentifier()
                .Apply(Tag.Builder.Create)
                .SetName(name)
                .SetWorkspaceId(workspaceId)
                .SetAt(timeService.CurrentDateTime)
                .SetSyncStatus(SyncStatus.SyncNeeded)
                .Build()
                .Apply(repository.Create)
                .Select(Tag.From);
    }
}
