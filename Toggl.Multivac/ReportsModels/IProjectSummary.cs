namespace Toggl.Multivac.ReportsModels
{
    public interface IProjectSummary
    {
        long UserId { get; }
        long? ProjectId { get; }
        long TrackedSeconds { get; }
        long? BillableSeconds { get; }
    }
}
