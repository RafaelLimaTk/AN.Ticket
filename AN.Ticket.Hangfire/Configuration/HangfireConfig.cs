using Hangfire;
using Hangfire.MySql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Transactions;

namespace AN.Ticket.Hangfire.Configuration;
public static class HangfireConfig
{
    public static void AddHangfireConfiguration(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddHangfire(config =>
        {
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170);
            config.UseSimpleAssemblyNameTypeSerializer();
            config.UseRecommendedSerializerSettings();
            config.UseStorage(new MySqlStorage(configuration.GetConnectionString("AtlasHanfireBd"),
                new MySqlStorageOptions
                {
                    TransactionIsolationLevel = IsolationLevel.ReadCommitted,
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    JobExpirationCheckInterval = TimeSpan.FromHours(1),
                    CountersAggregateInterval = TimeSpan.FromMinutes(5),
                    PrepareSchemaIfNecessary = true,
                    DashboardJobListLimit = 50000,
                    TransactionTimeout = TimeSpan.FromMinutes(1),
                    TablesPrefix = "Hangfire"
                }));
        });

        var workerCount = configuration.GetValue<int>("Hangfire:WorkerCount");
        if (workerCount <= 0)
            return;

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = workerCount;
            options.Queues = new[]
            {
                "default",
                Enums.TypeQueue.synchronization.ToString(),
                Enums.TypeQueue.support_ticket.ToString(),
                Enums.TypeQueue.ticket_response.ToString(),
                Enums.TypeQueue.general.ToString()
            };
        });
    }

    public static void UseHangfireConfiguration(
        this IApplicationBuilder app,
        IConfiguration configuration
    )
    {
        if (!configuration.GetValue<bool>("Hangfire:EnableDashboard"))
            return;

        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new HangfireAuthorizationFilter() },
            DisplayStorageConnectionString = true
        });
    }
}
