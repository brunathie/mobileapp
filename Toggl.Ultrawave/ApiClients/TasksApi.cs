using System;
using System.Collections.Generic;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.Models;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Serialization;

namespace Toggl.Ultrawave.ApiClients
{
    internal sealed class TasksApi : BaseApi, ITasksApi
    {
        private readonly TaskEndpoints endPoints;

        public TasksApi(Endpoints endPoints, IApiClient apiClient, IJsonSerializer serializer, Credentials credentials)
            : base(apiClient, serializer, credentials, endPoints.LoggedIn)
        {
            this.endPoints = endPoints.Tasks;
        }

        public IObservable<List<ITask>> GetAll()
            => CreateListObservable<Task, ITask>(endPoints.Get, AuthHeader);

        public IObservable<List<ITask>> GetAllSince(DateTimeOffset threshold)
            => CreateListObservable<Task, ITask>(endPoints.GetSince(threshold), AuthHeader);

        public IObservable<ITask> Create(ITask task)
        {
            var endPoint = endPoints.Post(task.WorkspaceId, task.ProjectId);
            var taskCopy = task as Task ?? new Task(task);
            var observable = CreateObservable(endPoint, AuthHeader, taskCopy, SerializationReason.Post);
            return observable;
        }
    }
}
