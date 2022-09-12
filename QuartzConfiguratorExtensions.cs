using Quartz;

namespace CoronaReportService;

public static class QuartzConfiguratorExtensions
{
    public static void AddJobWithTrigger<T>(
        this IServiceCollectionQuartzConfigurator quartz,
        IConfiguration  configuration) 
        where T : IJob
    {
        string jobName = typeof(T).Name;
        string configKey = $"Quartz:{jobName}";
        string cronExpression = configuration[configKey];
        if (string.IsNullOrEmpty(cronExpression))
        {
            throw new ArgumentException($"未找到配置项{configKey}");
        }
        else
        {
            quartz.AddJob<T>(opts => opts.WithIdentity(jobName))
                .AddTrigger(opts => opts.ForJob(jobName)
                    .WithIdentity($"{jobName}Trigger")
                    .WithCronSchedule(cronExpression)
                ).AddTrigger(opts => opts.ForJob(jobName)
                    .WithIdentity($"{jobName}InstantTrigger")
                    .StartNow()
                );
        }
    }


}