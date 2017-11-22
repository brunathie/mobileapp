namespace Toggl.PrimeRadiant
{
    public interface IAccessRestrictionStorage
    {
        void SetOutdatedClientVersion();
        void SetOutdatedApiVersion();
        void SetUnauthorizedAccess();
        bool IsAccessRestricted();
        bool IsClientOutdated();
        bool IsApiOutdated();
        bool IsUnauthorized();
    }
}
