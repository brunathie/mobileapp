using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Models
{
    internal partial class User
    {
        public User(IDatabaseUser user, long workspaceId)
            : this(user, SyncStatus.SyncNeeded, null)
        {
            DefaultWorkspaceId = workspaceId;
        }
    }

    internal static class UserExtensions
    {
        public static User With(this IDatabaseUser self, long workspaceId) => new User(self, workspaceId);
    }
}
