using Npgsql;

namespace HeartbeatCheckB
{
    public class RefreshService : BackgroundService
    {
        HttpClient _httpClient;
        int _refreshRateMillis;

        public RefreshService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _refreshRateMillis = StaticOutputs.RefreshRateInSeconds * 1000;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                var startTime = DateTime.UtcNow;

                Task[] tasks = { RefreshSite(), RefreshDb() };
                await Task.WhenAll(tasks).ConfigureAwait(false);

                StaticOutputs.LastCheckedTimeStampUTC = DateTime.UtcNow.ToString();
                var totalTime = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
                Console.WriteLine("Time taken to refresh: " + totalTime + "ms");
                await Task.Delay(_refreshRateMillis, stoppingToken).ConfigureAwait(false);
            }
        }

        async Task RefreshSite()
        {
            if (StaticOutputs.SiteToCheck is not null)
            {
                try
                {
                    var response = await _httpClient.GetAsync(StaticOutputs.SiteToCheck).ConfigureAwait(false);
                    StaticOutputs.CanReachSite = response.IsSuccessStatusCode ? "TRUE" : "FALSE";
                }
                catch (InvalidOperationException)
                {
                    StaticOutputs.CanReachSite = "ERROR: site URL in app settings is invalid or incomplete";
                }
            }
        }

        async Task RefreshDb()
        {
            try
            {
                using (NpgsqlConnection connection = new(StaticOutputs.DatabaseDetails))
                {
                    try
                    {
                        await connection.OpenAsync().ConfigureAwait(false);
                        NpgsqlCommand test = new("select 1", connection);
                        test.ExecuteScalar();
                        StaticOutputs.CanReachDatabase = "TRUE";
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("HOST NOT FOUND: " + ex.Message);
                        StaticOutputs.CanReachDatabase = "FALSE";
                    }
                    await connection.CloseAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: database connection details in app settings are invalid: " + ex.Message);
                StaticOutputs.CanReachDatabase = "ERROR: database connection details in app settings are invalid";
            }
        }
    }
}
