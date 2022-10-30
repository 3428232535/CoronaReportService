using System.Runtime.InteropServices;
using CoronaReportService;
using Quartz;
using Serilog;

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddQuartz(q =>
        {
            q.UseMicrosoftDependencyInjectionJobFactory();
            q.AddJobWithTrigger<ReportJob>(hostContext.Configuration);
        });
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
        services.AddHostedService<Worker>();
    });

#region Serilog

builder.UseSerilog((context, logger) =>
{
    logger.ReadFrom.Configuration(context.Configuration);
    logger.Enrich.FromLogContext();
});

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    builder.UseWindowsService();

#endregion

#region RegisterAsSystemService

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    builder.UseWindowsService();
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    builder.UseSystemd();

#endregion

await builder.Build().RunAsync();