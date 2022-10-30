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
        try
        {
            for (int i = 0; i < MaxRetryCount + 1; i++)
            {
                if (await TestConnectionAsync()) break;
                else
                {
                    if (i == MaxRetryCount) throw new System.Net.WebException("网络连接失败");
                    else await Task.Delay(TimeSpan.FromSeconds(10));
                }
            }
            IEnumerable<AccountConfiguration> accountConfigurations= LoadConfiguration();
            await Parallel.ForEachAsync(accountConfigurations,
                async (account, token) => await ReportAsync(account));
        }
        catch (Exception e)
        {
            _logger.LogError("上报失败,原因:{@e}", e);
            throw new JobExecutionException(e);
        }
    }

    private IEnumerable<AccountConfiguration> LoadConfiguration()
    {
        var accounts = _configuration.GetSection("AccountConfigurations").GetChildren();
        foreach (var account in accounts)
        {
            string username = account["username"];
            string password = account["password"];
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                throw new ArgumentException("账户未配置");
            yield return new(username, password);
        }
    }

    public record AccountConfiguration(string Username, string Password); 
    
    #region NetConnectionTest

    private static readonly int MaxRetryCount = 5;
    
    public static readonly HttpClient _testClient = new(new SocketsHttpHandler
    {
        UseCookies = false,AllowAutoRedirect = false,ConnectTimeout = TimeSpan.FromSeconds(3)
    });

    public static async Task<bool> TestConnectionAsync()
    {
        try
        {
            await _testClient.GetAsync("https://cn.bing.com/");
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion  

    private async Task ReportAsync(AccountConfiguration account)
    {
        WebService service = new WebService(account.Username, account.Password);
        _logger.LogInformation("正在登录");
        await service.AuthorizeAsync();
        _logger.LogDebug("登录成功");
        if (await service.TodayHasReported())
        {
            _logger.LogInformation("今日已经上报");
        }
        else
        {
            var content = await service.TodayReport();
            await service.SubmitReport(content);
            _logger.LogInformation("上报成功");
        }
    }
}