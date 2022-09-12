using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;
using HtmlAgilityPack;

namespace CoronaReportService;

public class WebService : AuthBase
{
    public WebService(string username, string password) : base(username, password)
    {
    }
    protected override async Task BeforeLogin()
    {
        HtmlDocument htmlDocument = new HtmlDocument();
        var indexResponse = await client.GetStreamAsync("http://i.cqu.edu.cn/new/index.html");
        htmlDocument.Load(indexResponse);
        var body = htmlDocument.DocumentNode.SelectSingleNode("//body");
        this.ServiceUrl = body.SelectSingleNode(".//div[@id='ampHasNoLogin']/a").Attributes["href"].Value;
    }
    protected override async Task LoginCallback(HttpResponseMessage response)
    {
        var adaperResponse = await client.GetAsync(response.Headers.Location);
        await client.GetAsync(adaperResponse.Headers.Location);
        await client.GetAsync("http://i.cqu.edu.cn/qljfwapp4/sys/lwStuReportEpidemic/index.do");
    }

    private async Task<JsonNode> LatestReport()
    {
        var payLoad = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            ["pageSize"] = "1",
            ["pageNumber"] = "1"
        });
        var reportResponse = await client.PostAsync("http://i.cqu.edu.cn/qljfwapp4/sys/lwStuReportEpidemic/modules/healthClock/getMyDailyReportDatas.do",payLoad);
        var content = await reportResponse.Content.ReadAsStreamAsync();
        var json = await JsonSerializer.DeserializeAsync<JsonNode>(content, Options);
        var latest = json["datas"]["getMyDailyReportDatas"]["rows"][0];
        return latest;
    }
    private async Task<string> TodayWID()
    {
        var widUrl = "http://i.cqu.edu.cn/qljfwapp4/sys/lwStuReportEpidemic/modules/healthClock/getMyTodayReportWid.do";
        var widResponse = await client.PostAsync(widUrl, new StringContent("pageNumber=1"));
        var widContent = await widResponse.Content.ReadAsStringAsync();
        var widJson = JsonNode.Parse(widContent);
        string wid = widJson["datas"]["getMyTodayReportWid"]["rows"][0]["WID"].GetValue<string>();
        return wid;
    }
    public async Task<JsonObject> TodayReport()
    {
        var latest = await LatestReport();
        var wid = await TodayWID();
        
        latest["WID"] = wid;
        latest["FILL_TIME"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        latest["NEED_CHECKIN_DATE"] = DateTime.Now.ToString("yyyy-MM-dd");
        latest["CREATED_AT"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var new_time = DateTime.Today.Add(new TimeSpan(23, 55, 0));
        latest["CZRQ"] = new_time.ToString("yyyy-MM-dd HH:mm:ss");
        return latest.AsObject();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>true if reported,false if not reported</returns>
    public async Task<bool> TodayHasReported()
    {
        var hasReportedResponse = await client.GetAsync("http://i.cqu.edu.cn/qljfwapp4/sys/lwStuReportEpidemic/modules/healthClock/getTodayHasReported.do");
        var hasReportedContent = await hasReportedResponse.Content.ReadAsStringAsync();
        var hasReportedJson = JsonNode.Parse(hasReportedContent);
        return hasReportedJson["datas"]["getTodayHasReported"]["totalSize"].GetValue<int>() != 0;
    }   
    
    /// <summary>
    /// 重复打卡会覆盖打卡记录,记录识别码为WID
    /// </summary>
    public async Task SubmitReport(JsonObject json)
    {
        var dict = json.ToDictionary(x => x.Key, x => x.Value?.ToString());
        var reportContent =  new FormUrlEncodedContent(dict);
        var reportUrl = "http://i.cqu.edu.cn/qljfwapp4/sys/lwStuReportEpidemic/modules/healthClock/T_HEALTH_DAILY_INFO_SAVE.do";
        var reportResponse = await client.PostAsync(reportUrl, reportContent);
    }
    
    private static JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowTrailingCommas = true,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };
}