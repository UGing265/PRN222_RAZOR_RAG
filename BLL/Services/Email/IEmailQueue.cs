namespace BLL.Services.Email;

public interface IEmailQueue
{
    void Enqueue(EmailJob job);
    int PendingCount { get; }
}
