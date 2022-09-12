using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CoronaReportService;

public abstract class AuthBase : IAsyncDisposable
{
    private readonly string _username;
    private readonly string _password;
    protected HttpClient client = new HttpClient();
    private readonly DES _des = DES.Create();
    
    public AuthBase(string username, string password)
    {
        _username = username;
        _password = password;
    }
    public string Username => _username;
    private byte[] PasswordBytes => Encoding.UTF8.GetBytes(_password);
    protected String ServiceUrl { get; set; }
    public ref HttpClient Client() => ref this.client;
    protected abstract Task BeforeLogin();
    protected abstract Task LoginCallback(HttpResponseMessage response);
    protected virtual async Task<HttpResponseMessage> LoginAsync()
    {
        string serviceResponse = await client.GetStringAsync(this.ServiceUrl);
        var content = await FillContent(serviceResponse);
        var loginResponse = await client.PostAsync(Service.LoginUrl, content);
        return loginResponse;
    }
    
    private Task<FormUrlEncodedContent> FillContent(string responseHtml)
    {
        string crypto = Regex.Match(responseHtml,"(?<=<p id=\"login-croypto\">)\\S+(?=</p>)").Value;
        string execution = Regex.Match(responseHtml, "(?<=<p id=\"login-page-flowkey\">)\\S+(?=</p>)").Value;
        _des.Key = Convert.FromBase64String(crypto);
        byte[] cipherBytes = _des.EncryptEcb(PasswordBytes,PaddingMode.PKCS7);
        string cipherStr = Convert.ToBase64String(cipherBytes);
        var content = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            {"username", this.Username},
            {"type", "UsernamePassword"},
            {"_eventId", "submit"},
            {"execution", execution},
            {"croypto", crypto},
            {"password", cipherStr}
        });
        return Task.FromResult(content);
    }

    public async Task AuthorizeAsync()
    {
        await this.BeforeLogin();
        var response = await this.LoginAsync();
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new AuthException("Password error");
        await this.LoginCallback(response);
    }
    
    public Task LogoutAsync() => client.GetAsync(Service.LogoutUrl);

    public async ValueTask DisposeAsync() => await LogoutAsync();
}