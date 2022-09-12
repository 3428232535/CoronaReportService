namespace CoronaReportService;

[Serializable]
public class AuthException : Exception 
{
    public AuthException() { }
    public AuthException(string msg) :base(msg){ }
    public AuthException(string msg, Exception inner) :base(msg,inner){ }
}