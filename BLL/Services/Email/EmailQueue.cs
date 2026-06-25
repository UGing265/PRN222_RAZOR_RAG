using System.Threading.Channels;

namespace BLL.Services.Email;

public sealed class EmailQueue : IEmailQueue
{
    private readonly Channel<EmailJob> _channel;

    public EmailQueue()
    {
        _channel = Channel.CreateUnbounded<EmailJob>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public int PendingCount => _channel.Reader.Count;

    public void Enqueue(EmailJob job)
    {
        if (!_channel.Writer.TryWrite(job))
            throw new InvalidOperationException("Failed to enqueue email job.");
    }

    internal ChannelReader<EmailJob> Reader => _channel.Reader;
}
