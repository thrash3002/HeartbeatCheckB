using System.Text.Json;

namespace HeartbeatCheckB
{
    public static class StaticOutputs
    {
        public static string? SiteToCheck;
        public static string CanReachSite = "ERROR: Failed to locate site URL in app settings"; // default

        public static string? DatabaseDetails;
        public static string CanReachDatabase = "ERROR: Failed to locate database connection details in app settings"; // default

        public static int RefreshRateInSeconds;
        public static string? LastCheckedTimeStampUTC;

        public static string Get()
        {
            return JsonSerializer.Serialize(new
            {
                SiteToCheck,
                CanReachSite,
                CanReachDatabase,
                LastCheckedTimeStampUTC,
            });
        }
    }
}
