using System.Text.Json.Nodes;
using System.Xml;
using Quartz;

namespace CoronaReportService;

public class ReportJob :IJob
{
    private readonly ILogger<ReportJob> _logger;
    private readonly IConfiguration _configuration;

    public ReportJob(ILogger<ReportJob> logger,IConfiguration configuration) {
        _logger = logger;
        _configuration = configuration;
    }
    public async Task Execute(IJobExecutionContext context)
    {
        (string username,string password) = LoadConfiguration();
        WebService service = new WebService(username, password);
        _logger.LogInformation("正在登录");
        await service.AuthorizeAsync();
        _logger.LogInformation("登录成功");
        if (await service.TodayHasReported())
        {
            _logger.LogInformation("今日已经上报");
            
        }
        else
        {
            _logger.LogInformation("正在获取上报信息");
            var content = await service.TodayReport();
            _logger.LogInformation("成功获取,正在上报");
            await service.SubmitReport(content);
            _logger.LogInformation("上报成功");
        }
    }

    private (string Username, string Password) LoadConfiguration() {
        string username = _configuration["Account:username"];
        string password = _configuration["Account:password"];
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            throw new ArgumentException("账户未配置");
        return (username, password);
    }
}