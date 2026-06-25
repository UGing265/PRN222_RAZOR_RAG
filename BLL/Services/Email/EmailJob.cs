namespace BLL.Services.Email;

public sealed record EmailJob(
    string To,
    string Subject,
    string Body,
    int RetryCount = 0);
