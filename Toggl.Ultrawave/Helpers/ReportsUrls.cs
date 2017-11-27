using System;
namespace Toggl.Ultrawave.Helpers
{
    public static class ReportsUrls
    {
        private const string productionBaseUrl = "https://toggl.com/reports/api/v3/";
        private const string stagingBaseUrl = "https://toggl.space/reports/api/v3/";

        public static Uri ForEnvironment(ApiEnvironment environment)
        {
            switch (environment)
            {
                case ApiEnvironment.Staging:
                    return new Uri(stagingBaseUrl);
                case ApiEnvironment.Production:
                    return new Uri(productionBaseUrl);
                default:
                    throw new ArgumentOutOfRangeException(nameof(environment), environment, "Unknown api environment.");
            }
        }
    }
}
