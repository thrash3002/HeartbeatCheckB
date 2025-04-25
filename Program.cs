using HeartbeatCheckB;

var builder = WebApplication.CreateBuilder(args);

StaticOutputs.DatabaseDetails = 
    builder.Configuration.GetConnectionString(
        builder.Environment.ContentRootPath.Equals("/app") ? 
        "DockerDBConnectionString" : 
        "DBConnectionString");

StaticOutputs.SiteToCheck = builder.Configuration.GetValue<string>("SiteToCheck");
var useDefaultRefreshRate = 
    !int.TryParse(
        builder.Configuration.GetValue<string>("RefreshRateInSeconds"), 
        out StaticOutputs.RefreshRateInSeconds);
if(useDefaultRefreshRate)
{
    StaticOutputs.RefreshRateInSeconds = 10; // default
}
Console.WriteLine("Setting refresh rate to every " + StaticOutputs.RefreshRateInSeconds + " seconds");

builder.Services.AddHttpClient();
builder.Services.AddHostedService<RefreshService>();

var app = builder.Build();

app.MapGet("/", () => StaticOutputs.Get());

app.Run();
