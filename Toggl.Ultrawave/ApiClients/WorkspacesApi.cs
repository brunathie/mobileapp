using System;
using System.Collections.Generic;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.Models;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Serialization;

namespace Toggl.Ultrawave.ApiClients
{
    internal sealed class WorkspacesApi : BaseApi, IWorkspacesApi
    {
        private readonly WorkspaceEndpoints endPoints;

        public WorkspacesApi(Endpoints endPoints, IApiClient apiClient, IJsonSerializer serializer,
            Credentials credentials)
            : base(apiClient, serializer, credentials, endPoints.LoggedIn)
        {
            this.endPoints = endPoints.Workspaces;
        }

        public IObservable<List<IWorkspace>> GetAll()
            => CreateListObservable<Workspace, IWorkspace>(endPoints.Get, AuthHeader);

        public IObservable<IWorkspace> GetById(long id)
        {
            var endpoint = endPoints.GetById(id);
            var observable = CreateObservable<Workspace>(endpoint, AuthHeader);
            return observable;
        }
    }
}
