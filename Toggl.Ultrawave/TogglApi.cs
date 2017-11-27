using System.Net.Http;
using ModernHttpClient;
using Toggl.Multivac;
using Toggl.Ultrawave.ApiClients;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Serialization;
using static System.Net.DecompressionMethods;

namespace Toggl.Ultrawave
{
    public sealed class TogglApi : ITogglApi
    {
        public TogglApi(ApiConfiguration configuration, HttpClientHandler handler = null)
        {
            Ensure.Argument.IsNotNull(configuration, nameof(configuration));

            var httpHandler = handler ?? new NativeMessageHandler { AutomaticDecompression = GZip | Deflate };
            var httpClient = new HttpClient(httpHandler);

            var userAgent = configuration.UserAgent;
            var credentials = configuration.Credentials;
            var serializer = new JsonSerializer();
            var apiClient = new ApiClient(httpClient, userAgent);
            var endpoints = new Endpoints(configuration.Environment);

            Tags = new TagsApi(endpoints, apiClient, serializer, credentials);
            User = new UserApi(endpoints, apiClient, serializer, credentials);
            Tasks = new TasksApi(endpoints, apiClient, serializer, credentials);
            Status = new StatusApi(endpoints, apiClient);
            Clients = new ClientsApi(endpoints, apiClient, serializer, credentials);
            Projects = new ProjectsApi(endpoints, apiClient, serializer, credentials);
            Workspaces = new WorkspacesApi(endpoints, apiClient, serializer, credentials);
            TimeEntries = new TimeEntriesApi(endpoints, apiClient, serializer, credentials, userAgent);
            WorkspaceFeatures = new WorkspaceFeaturesApi(endpoints, apiClient, serializer, credentials);
        }

        public ITagsApi Tags { get; }
        public IUserApi User { get; }
        public ITasksApi Tasks { get; }
        public IStatusApi Status { get; }
        public IClientsApi Clients { get; }
        public IProjectsApi Projects { get; }
        public IWorkspacesApi Workspaces { get; }
        public ITimeEntriesApi TimeEntries { get; }
        public IWorkspaceFeaturesApi WorkspaceFeatures { get; }
    }
}
