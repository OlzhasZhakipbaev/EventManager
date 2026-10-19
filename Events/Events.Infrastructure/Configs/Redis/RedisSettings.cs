namespace Events.Infrastructure.Configs.Redis;

public class RedisSettings
{
    public const string SectionName = "Redis";
    
    public string ConnectionString { get; set; } = "";
    public int EventTtlMinutes { get; set; } = 5;
    public int TopEventsTtlMinutes { get; set; } = 5;
    public long ConnectTimeout { get; set; }
    public long SyncTimeout { get; set; }
    public bool AbortOnConnectFail { get; set; }
}