using System;
using Foundation;
using Toggl.PrimeRadiant;

namespace Toggl.Daneel.Services
{
    internal class UserDataAccessRestrictionStorage : IAccessRestrictionStorage
    {
        private readonly Version version;
        private const string OutdatedClient = "OutdatedClient";
        private const string OutdatedApi = "OutdatedApi";
        private const string UnauthorizedAccess = "UnauthorizedAccess";

        public UserDataAccessRestrictionStorage(Version version)
        {
            this.version = version;
        }

        public void SetClientOutdated()
            => NSUserDefaults.StandardUserDefaults.SetString(version.ToString(), OutdatedClient);

        public void SetApiOutdated()
            => NSUserDefaults.StandardUserDefaults.SetString(version.ToString(), OutdatedApi);

        public void SetUnauthorizedAccess()
            => NSUserDefaults.StandardUserDefaults.SetBool(true, UnauthorizedAccess);

        public void ClearUnauthorizedAccess()
            => NSUserDefaults.StandardUserDefaults.SetBool(false, UnauthorizedAccess);

        public bool IsClientOutdated()
            => isOutdated(OutdatedClient);

        public bool IsApiOutdated()
            => isOutdated(OutdatedApi);

        public bool IsUnauthorized()
            => NSUserDefaults.StandardUserDefaults.BoolForKey(UnauthorizedAccess);

        private bool isOutdated(string key)
        {
            var storedVersion = getStoredVersion(key);
            return storedVersion != null && version <= storedVersion;
        }

        private Version getStoredVersion(string key)
        {
            var stored = NSUserDefaults.StandardUserDefaults.StringForKey(key);
            return stored == null ? null : Version.Parse(stored);
        }

    }
}
