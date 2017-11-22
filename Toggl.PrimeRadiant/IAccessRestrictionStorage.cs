namespace Toggl.PrimeRadiant
{
    public interface IAccessRestrictionStorage
    {
        void SetClientOutdated();
        void SetApiOutdated();
        void SetUnauthorizedAccess();
        void ClearUnauthorizedAccess();
        bool IsClientOutdated();
        bool IsApiOutdated();
        bool IsUnauthorized();
    }
}
