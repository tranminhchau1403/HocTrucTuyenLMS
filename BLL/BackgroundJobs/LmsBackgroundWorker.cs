using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DAL;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BLL.BackgroundJobs
{
    public record BackgroundEmailItem(string RecipientEmail, string Subject, string HtmlBody, DateTime EnqueuedAtUtc);

    public interface IBackgroundTaskQueue
    {
        bool TryQueueEmailNotification(string recipientEmail, string subject, string htmlBody);
        ValueTask<BackgroundEmailItem> DequeueEmailAsync(CancellationToken cancellationToken);
    }

    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        public static readonly BackgroundTaskQueue Shared = new();

        private readonly Channel<BackgroundEmailItem> _emailChannel;

        public BackgroundTaskQueue()
        {
            var options = new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            };
            _emailChannel = Channel.CreateBounded<BackgroundEmailItem>(options);
        }

        public bool TryQueueEmailNotification(string recipientEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(recipientEmail))
                return false;

            return _emailChannel.Writer.TryWrite(new BackgroundEmailItem(
                recipientEmail.Trim(),
                subject,
                htmlBody,
                DateTime.UtcNow));
        }

        public ValueTask<BackgroundEmailItem> DequeueEmailAsync(CancellationToken cancellationToken)
        {
            return _emailChannel.Reader.ReadAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Background Worker (IHostedService / BackgroundService) chuẩn Bài 11 & Phụ lục B Giáo trình:
    /// 1. Xử lý hàng đợi gửi Email / Thông báo bất đồng bộ qua Channel&lt;T&gt;
    /// 2. Chạy tác vụ định kỳ (PeriodicTimer) dọn dẹp RefreshToken hết hạn
    /// </summary>
    public class LmsBackgroundWorker : BackgroundService
    {
        private readonly IBackgroundTaskQueue _queue;
        private readonly ILogger<LmsBackgroundWorker> _logger;

        public LmsBackgroundWorker(IBackgroundTaskQueue queue, ILogger<LmsBackgroundWorker> logger)
        {
            _queue = queue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LmsBackgroundWorker đã khởi động: sẵn sàng xử lý hàng đợi Email và dọn dẹp phiên định kỳ.");

            var emailProcessorTask = ProcessEmailQueueAsync(stoppingToken);
            var cleanupTimerTask = RunPeriodicTokenCleanupAsync(stoppingToken);

            await Task.WhenAll(emailProcessorTask, cleanupTimerTask);
        }

        private async Task ProcessEmailQueueAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var item = await _queue.DequeueEmailAsync(stoppingToken);

                    // Ghi nhận kết quả gửi email vào thư mục Logs/Emails để phục vụ kiểm thử & kiểm toán
                    string logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
                    Directory.CreateDirectory(logDir);
                    string logFile = Path.Combine(logDir, "background_emails.log");
                    string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] TO: {item.RecipientEmail} | SUBJECT: {item.Subject} | BODY: {item.HtmlBody}{Environment.NewLine}";
                    await File.AppendAllTextAsync(logFile, entry, stoppingToken);

                    _logger.LogInformation("[BackgroundQueue] Đã gửi email thông báo tới {Email} - Tiêu đề: {Subject}",
                        item.RecipientEmail, item.Subject);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi xử lý hàng đợi Email trong LmsBackgroundWorker.");
                }
            }
        }

        private async Task RunPeriodicTokenCleanupAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromHours(6));
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    var refreshDal = new RefreshTokenDAL();
                    int removed = refreshDal.DeleteExpiredTokens();
                    if (removed > 0)
                    {
                        _logger.LogInformation("[PeriodicCleanup] Đã dọn dẹp {Count} RefreshToken hết hạn.", removed);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể chạy tác vụ dọn dẹp RefreshToken định kỳ.");
                }
            }
        }
    }
}
