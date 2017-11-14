using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.Helpers;

namespace Toggl.Ultrawave.Tests.Integration.Helper
{
    internal sealed class SubscriptionPlanActivator
    {
        private readonly Dictionary<ITogglApi, PricingPlans> planOfDefaultWorkspaceByApi = new Dictionary<ITogglApi, PricingPlans>();
        private readonly Dictionary<ITogglApi, IUser> userByApi = new Dictionary<ITogglApi, IUser>();
        private readonly Dictionary<long, PricingPlans> planOfDefaultWorkspaceById = new Dictionary<long, PricingPlans>();

        public async Task EnsureDefaultWorkspaceIsOnPlan(ITogglApi api, PricingPlans plan)
        {
            if (planOfDefaultWorkspaceByApi.TryGetValue(api, out var lastPlan) && lastPlan == plan)
                return;

            planOfDefaultWorkspaceByApi[api] = plan;

            if (!userByApi.TryGetValue(api, out var user))
            {
                user = await api.User.Get();
                userByApi[api] = user;
            }

            await EnsureDefaultWorkspaceIsOnPlan(user, plan);
        }

        public Task EnsureDefaultWorkspaceIsOnPlan(IUser user, PricingPlans plan)
            => EnsureWorkspaceIsOnPlan(user, user.DefaultWorkspaceId, plan);

        public async Task EnsureWorkspaceIsOnPlan(IUser user, long workspaceId, PricingPlans plan)
        {
            if (planOfDefaultWorkspaceById.TryGetValue(workspaceId, out var lastPlan) && lastPlan == plan)
                return;

            planOfDefaultWorkspaceById[workspaceId] = plan;

            await WorkspaceHelper.SetSubscription(user, workspaceId, plan);
        }
    }
}
