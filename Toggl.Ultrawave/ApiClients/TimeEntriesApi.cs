using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.Models;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Serialization;

namespace Toggl.Ultrawave.ApiClients
{
    internal sealed class TimeEntriesApi : BaseApi, ITimeEntriesApi
    {
        private readonly UserAgent userAgent;
        private readonly TimeEntryEndpoints endPoints;

        public TimeEntriesApi(Endpoints endPoints, IApiClient apiClient, IJsonSerializer serializer, 
            Credentials credentials, UserAgent userAgent)
            : base(apiClient, serializer, credentials, endPoints.LoggedIn)
        {
            this.userAgent = userAgent;
            this.endPoints = endPoints.TimeEntries;
        }

        public IObservable<List<ITimeEntry>> GetAll()
            => CreateListObservable<TimeEntry, ITimeEntry>(endPoints.Get, AuthHeader);

        public IObservable<List<ITimeEntry>> GetAllSince(DateTimeOffset threshold)
            => CreateListObservable<TimeEntry, ITimeEntry>(endPoints.GetSince(threshold), AuthHeader);

        public IObservable<ITimeEntry> Create(ITimeEntry timeEntry)
            => pushTimeEntry(endPoints.Post(timeEntry.WorkspaceId), timeEntry, SerializationReason.Post);

        public IObservable<ITimeEntry> Update(ITimeEntry timeEntry)
            => pushTimeEntry(endPoints.Put(timeEntry.WorkspaceId, timeEntry.Id), timeEntry, SerializationReason.Default);

        public IObservable<Unit> Delete(ITimeEntry timeEntry)
            => CreateObservable<ITimeEntry>(endPoints.Delete(timeEntry.WorkspaceId, timeEntry.Id), AuthHeader)
                .SingleAsync()
                .Select(_ => Unit.Default);

        private IObservable<ITimeEntry> pushTimeEntry(Endpoint endPoint, ITimeEntry timeEntry, SerializationReason reason)
        {
            var timeEntryCopy = timeEntry as TimeEntry ?? new TimeEntry(timeEntry);
            if (reason == SerializationReason.Post)
            {
                timeEntryCopy.CreatedWith = userAgent.ToString();
            }

            var observable = CreateObservable(endPoint, AuthHeader, timeEntryCopy, reason);
            return observable;
        }
    }
}
