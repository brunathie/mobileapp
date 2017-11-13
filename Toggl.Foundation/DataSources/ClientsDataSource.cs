using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Foundation.Models;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.DataSources
{
    public sealed class ClientsDataSource : IClientsSource
    {
        private readonly IIdProvider idProvider;
        private readonly ITimeService timeService;
        private readonly IRepository<IDatabaseClient> repository;

        public ClientsDataSource(IIdProvider idProvider, IRepository<IDatabaseClient> repository, ITimeService timeService)
        {
            Ensure.Argument.IsNotNull(idProvider, nameof(idProvider));
            Ensure.Argument.IsNotNull(repository, nameof(repository));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));

            this.repository = repository;
            this.idProvider = idProvider;
            this.timeService = timeService;
        }

        public IObservable<IDatabaseClient> Create(string name, long workspaceId)
            => idProvider.GetNextIdentifier()
                .Apply(Client.Builder.Create)
                .SetName(name)
                .SetWorkspaceId(workspaceId)
                .SetAt(timeService.CurrentDateTime)
                .SetSyncStatus(SyncStatus.SyncNeeded)
                .Build()
                .Apply(repository.Create)
                .Select(Client.From);

        public IObservable<IEnumerable<IDatabaseClient>> GetAllInWorkspace(long workspaceId)
            => repository
                .GetAll(c => c.WorkspaceId == workspaceId)
                .Select(clients => clients.Select(Client.From));

        public IObservable<IDatabaseClient> GetById(long id)
            => repository.GetById(id).Select(Client.From);
    }
}
