using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.Helpers;
using Toggl.Ultrawave.Models;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Serialization;
using Task = System.Threading.Tasks.Task;

namespace Toggl.Ultrawave.Tests.Integration.Helper
{
    internal static class WorkspaceHelper
    {
        public static async Task<Workspace> CreateFor(IUser user)
        {
            var newWorkspaceName = $"{Guid.NewGuid()}";
            var json = $"{{\"name\": \"{newWorkspaceName}\"}}";

            var responseBody = await makeRequest("https://toggl.space/api/v9/workspaces", HttpMethod.Post, user, json);

            var jsonSerializer = new JsonSerializer();
            return jsonSerializer.Deserialize<Workspace>(responseBody);
        }

        public static async Task SetSubscription(IUser user, long workspaceId, PricingPlans plan)
        {
            var json = $"{{\"pricing_plan_id\":{(int)plan}}}";

            await makeRequest($"https://toggl.space/api/v9/workspaces/{workspaceId}/subscriptions", HttpMethod.Post, user, json);
        }

        public static async Task<List<int>> GetAllAvailablePricingPlans(IUser user)
        {
            var response = await makeRequest($"https://toggl.space/api/v9/workspaces/{user.DefaultWorkspaceId}/plans", HttpMethod.Get, user, null);
            var matches = Regex.Matches(response, "\\\"pricing_plan_id\\\":\\s*(?<id>\\d+),");
            return matches.Cast<Match>().SelectMany(match => match.Groups["id"].Captures.Cast<Capture>().Select(capture => int.Parse(capture.Value))).ToList();
        }

        private static async Task<string> makeRequest(string endpoint, HttpMethod method, IUser user, string json)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

            var requestMessage = AuthorizedRequestBuilder.CreateRequest(
                Credentials.WithApiToken(user.ApiToken), endpoint, method);

            if (json != null)
                requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using (var client = new HttpClient())
            {
                var response = await client.SendAsync(requestMessage);
                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
