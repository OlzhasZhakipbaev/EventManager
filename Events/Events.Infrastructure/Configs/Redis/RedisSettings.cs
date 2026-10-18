namespace Events.Infrastructure.Configs.Redis;

public class ConfigurationOptions
{
    public string Endpoint { get; set; }
    public string Password { get; set; }
    public string ConnectTimeout { get; set; }
    public string SyncTimeout { get; set; }
    public bool AbortOnConnectFail { get; set; }
}