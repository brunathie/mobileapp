using System;
using System.Collections.Generic;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.Models;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Serialization;

namespace Toggl.Ultrawave.ApiClients
{
    internal sealed class WorkspaceFeaturesApi : BaseApi, IWorkspaceFeaturesApi
    {
        private readonly WorkspaceFeaturesEndpoints endPoints;

        public WorkspaceFeaturesApi(Endpoints endPoints, IApiClient apiClient, IJsonSerializer serializer,
            Credentials credentials)
            : base(apiClient, serializer, credentials, endPoints.LoggedIn)
        {
            this.endPoints = endPoints.WorkspaceFeatures;
        }

        public IObservable<List<IWorkspaceFeatureCollection>> GetAll()
            => CreateListObservable<WorkspaceFeatureCollection, IWorkspaceFeatureCollection>(endPoints.Get, AuthHeader);

    }
}
