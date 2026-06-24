namespace BLL.Services.Email;

public class EmailQueueOptions
{
    public const string SectionName = "EmailQueue";
    public int ThrottleDelayMs { get; set; } = 200;
    public int MaxRetries { get; set; } = 3;
}
