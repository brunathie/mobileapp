using System;
using System.Collections.Generic;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.Models;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Serialization;

namespace Toggl.Ultrawave.ApiClients
{
    internal sealed class TagsApi : BaseApi, ITagsApi
    {
        private readonly TagEndpoints endPoints;

        public TagsApi(Endpoints endPoints, IApiClient apiClient, IJsonSerializer serializer, Credentials credidentials)
            : base(apiClient, serializer, credidentials, endPoints.LoggedIn)
        {
            this.endPoints = endPoints.Tags;
        }

        public IObservable<List<ITag>> GetAll()
            => CreateListObservable<Tag, ITag>(endPoints.Get, AuthHeader);

        public IObservable<List<ITag>> GetAllSince(DateTimeOffset threshold)
            => CreateListObservable<Tag, ITag>(endPoints.GetSince(threshold), AuthHeader);

        public IObservable<ITag> Create(ITag tag)
        {
            var endPoint = endPoints.Post(tag.WorkspaceId);
            var tagCopy = tag as Tag ?? new Tag(tag);
            var observable = CreateObservable(endPoint, AuthHeader, tagCopy, SerializationReason.Post);
            return observable;
        }
    }
}
