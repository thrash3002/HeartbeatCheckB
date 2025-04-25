using Npgsql;
using System.Net.Http;

namespace HeartbeatCheckB
{
    public class RefreshService : BackgroundService
    {
        IHttpClientFactory _httpClientFactory;
        int _refreshRateMillis;

        public RefreshService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _refreshRateMillis = StaticOutputs.RefreshRateInSeconds * 1000;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                var startTime = DateTime.UtcNow;
                var delayTask = Task.Delay(_refreshRateMillis, stoppingToken).ConfigureAwait(false);

                List<Task> tasks = RefreshSites();
                tasks.Add(RefreshDb());
                await Task.WhenAll(tasks).ConfigureAwait(false);
                
                StaticOutputs.LastCheckedTimeStampUTC = DateTime.UtcNow.ToString();
                var totalTime = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
                Console.WriteLine("Total Time taken to refresh: " + (int)totalTime + "ms");
                //await Task.Delay(_refreshRateMillis, stoppingToken).ConfigureAwait(false);
                await delayTask;
                totalTime = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
                Console.WriteLine();
                Console.WriteLine("Total Time since last refresh: " + (int)totalTime + "ms");
            }
        }
        List<Task> RefreshSites()
        {
            var tasks = new List<Task>();
            if (StaticOutputs.CanReachSites is not null)
            {
                foreach (var site in StaticOutputs.CanReachSites.Keys)
                {
                    tasks.Add(RefreshSite(site));
                }
            }
            return tasks;
        }

        async Task RefreshSite(string site)
        {
            if (StaticOutputs.CanReachSites is not null &&
                site is not null)
            {
                var startTime = DateTime.UtcNow;
                try
                {
                    var _httpClient = _httpClientFactory.CreateClient();
                    var response = await _httpClient.GetAsync(site).ConfigureAwait(false);
                    StaticOutputs.CanReachSites[site] = response.IsSuccessStatusCode ? "TRUE" : "FALSE";
                }
                catch (InvalidOperationException)
                {
                    StaticOutputs.CanReachSites[site] = "ERROR: site URL in app settings is invalid or incomplete";
                }
                var totalTime = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
                Console.WriteLine("Time taken to refresh " + site + ": " + (int)totalTime + "ms");
            }
        }

        async Task RefreshDb()
        {
            var startTime = DateTime.UtcNow;
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
            var totalTime = DateTime.UtcNow.Subtract(startTime).TotalMilliseconds;
            Console.WriteLine("Time taken to refresh database: " + (int)totalTime + "ms");
        }
    }
}
