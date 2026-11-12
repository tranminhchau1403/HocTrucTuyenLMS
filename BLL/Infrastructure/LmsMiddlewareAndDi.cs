using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using BLL.BackgroundJobs;
using BLL.Caching;
using BLL.Security;
using DAL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Models;
using Serilog;
using Serilog.Context;

namespace BLL.Infrastructure
{
    /// <summary>
    /// Middleware gắn Correlation ID (X-Correlation-Id / X-Trace-Id) vào HttpContext, Response Header và Serilog LogContext
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private const string HeaderName = "X-Correlation-Id";
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string correlationId = context.Request.Headers.TryGetValue(HeaderName, out var vals) && !string.IsNullOrWhiteSpace(vals)
                ? vals.ToString()
                : (context.Request.Headers.TryGetValue("X-Trace-Id", out var traceVals) && !string.IsNullOrWhiteSpace(traceVals)
                    ? traceVals.ToString()
                    : Guid.NewGuid().ToString("N"));

            context.TraceIdentifier = correlationId;
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await _next(context);
            }
        }
    }

    /// <summary>
    /// Global Exception Middleware chuẩn Bài 5 & Phụ lục A Giáo trình:
    /// Bắt mọi ngoại lệ, trả về JSON chuẩn RFC 7807 ProblemDetails kết hợp ApiResponse, ẩn StackTrace ở Production
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var (statusCode, title, message, errors) = MapException(ex);

            _logger.LogError(ex, "GlobalExceptionMiddleware bắt ngoại lệ tại {Method} {Path} [TraceId: {TraceId}] -> HTTP {StatusCode}: {Message}",
                context.Request.Method, context.Request.Path, context.TraceIdentifier, statusCode, message);

            context.Response.ContentType = "application/problem+json; charset=utf-8";
            context.Response.StatusCode = statusCode;

            var payload = new
            {
                // Chuẩn RFC 7807 / RFC 9457 ProblemDetails
                type = $"https://httpstatuses.com/{statusCode}",
                title,
                status = statusCode,
                detail = message,
                instance = context.Request.Path.Value,
                traceId = context.TraceIdentifier,

                // Chuẩn ApiResponse thống nhất Phụ lục A
                success = false,
                statusCode,
                message,
                errors,
                developerException = _env.IsDevelopment() ? ex.GetType().Name : null
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }

        private static (int StatusCode, string Title, string Message, object? Errors) MapException(Exception ex)
        {
            return ex switch
            {
                AppException appEx => (appEx.StatusCode, GetTitleForStatus(appEx.StatusCode), appEx.Message, appEx.Errors),
                ArgumentException argEx => (400, "Bad Request", argEx.Message, null),
                InvalidOperationException invEx when invEx.Message.Contains("không có quyền", StringComparison.OrdinalIgnoreCase)
                                                  || invEx.Message.Contains("của giảng viên khác", StringComparison.OrdinalIgnoreCase)
                                                  || invEx.Message.Contains("của học viên khác", StringComparison.OrdinalIgnoreCase)
                    => (403, "Forbidden", invEx.Message, null),
                InvalidOperationException invEx when invEx.Message.Contains("đã tồn tại", StringComparison.OrdinalIgnoreCase)
                                                  || invEx.Message.Contains("đã được sử dụng", StringComparison.OrdinalIgnoreCase)
                                                  || invEx.Message.Contains("đã đăng ký", StringComparison.OrdinalIgnoreCase)
                    => (409, "Conflict", invEx.Message, null),
                InvalidOperationException invEx when invEx.Message.Contains("không tồn tại", StringComparison.OrdinalIgnoreCase)
                                                  || invEx.Message.Contains("Không tìm thấy", StringComparison.OrdinalIgnoreCase)
                    => (404, "Not Found", invEx.Message, null),
                InvalidOperationException invEx => (400, "Bad Request", invEx.Message, null),
                SqlException sqlEx when sqlEx.Number >= 50000 => (400, "Business Rule Violation", sqlEx.Message, null),
                SqlException sqlEx when sqlEx.Number == 2601 || sqlEx.Number == 2627 => (409, "Conflict", "Dữ liệu bị trùng lặp trong hệ thống.", null),
                _ => (500, "Internal Server Error", "Đã xảy ra lỗi hệ thống nội bộ. Vui lòng thử lại sau.", null)
            };
        }

        private static string GetTitleForStatus(int status) => status switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            409 => "Conflict",
            422 => "Unprocessable Entity",
            429 => "Too Many Requests",
            _ => "Server Error"
        };
    }

    /// <summary>
    /// Kiểm tra sức khỏe kết nối SQL Server thực tế cho endpoint /healthz và /health/ready
    /// </summary>
    public class SqlServerConnectionHealthCheck : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, System.Threading.CancellationToken cancellationToken = default)
        {
            try
            {
                await using var conn = KetNoi.GetConnection();
                await conn.OpenAsync(cancellationToken);
                await using var cmd = new SqlCommand("SELECT 1", conn);
                await cmd.ExecuteScalarAsync(cancellationToken);
                return HealthCheckResult.Healthy("SQL Server QuanLyHocTrucTuyen đang hoạt động tốt.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Degraded("Không thể kết nối SQL Server.", ex);
            }
        }
    }

    public static class LmsServiceExtensions
    {
        public static WebApplicationBuilder AddLmsCoreServices(this WebApplicationBuilder builder, string serviceName)
        {
            // 1. Cấu hình Serilog Structured Logging (Console + Rolling File)
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Service", serviceName)
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] [CorrId:{CorrelationId}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: System.IO.Path.Combine(AppContext.BaseDirectory, "Logs", $"app_log_{serviceName}_.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14,
                    shared: true)
                .CreateLogger();

            builder.Host.UseSerilog();

            // 2. Cấu hình ConnectionString
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                KetNoi.ConnectionString = connectionString;
            }

            // 3. Cấu hình MemoryCache + Redis DistributedCache (HybridCacheService)
            builder.Services.AddMemoryCache();
            var redisConn = builder.Configuration["Redis:ConnectionString"]
                ?? Environment.GetEnvironmentVariable("Redis__ConnectionString");
            if (!string.IsNullOrWhiteSpace(redisConn))
            {
                builder.Services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConn;
                    options.InstanceName = "CTLMS:";
                });
            }

            builder.Services.AddSingleton<ICacheService, HybridCacheService>();

            // 4. Đăng ký DI cho toàn bộ tầng DAL (Repositories)
            builder.Services.AddSingleton<IDbConnectionFactory>(_ => new SqlConnectionFactory(KetNoi.ConnectionString));
            builder.Services.AddScoped<INguoiDungRepository, NguoiDungDAL>();
            builder.Services.AddScoped<ISinhVienRepository, SinhVienDAL>();
            builder.Services.AddScoped<IGiangVienRepository, GiangVienDAL>();
            builder.Services.AddScoped<IDanhMucRepository, DanhMucDAL>();
            builder.Services.AddScoped<IKhoaHocRepository, KhoaHocDAL>();
            builder.Services.AddScoped<IBaiGiangRepository, BaiGiangDAL>();
            builder.Services.AddScoped<IDangKyKhoaHocRepository, DangKyKhoaHocDAL>();
            builder.Services.AddScoped<IBaiKiemTraRepository, BaiKiemTraDAL>();
            builder.Services.AddScoped<IKetQuaKiemTraRepository, KetQuaKiemTraDAL>();
            builder.Services.AddScoped<IThongBaoRepository, ThongBaoDAL>();
            builder.Services.AddScoped<IBaoCaoRepository, BaoCaoDAL>();
            builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenDAL>();
            builder.Services.AddScoped<IAuditLogRepository, AuditLogDAL>();
            builder.Services.AddScoped<ICauHoiRepository, CauHoiDAL>();
            builder.Services.AddScoped<ITienDoBaiGiangRepository, TienDoBaiGiangDAL>();
            builder.Services.AddScoped<IPhienLamBaiRepository, PhienLamBaiDAL>();

            // 5. Đăng ký DI cho toàn bộ tầng BLL (Business Services)
            builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
            builder.Services.AddScoped<NguoiDungBLL>();
            builder.Services.AddScoped<SinhVienBLL>();
            builder.Services.AddScoped<GiangVienBLL>();
            builder.Services.AddScoped<DanhMucBLL>();
            builder.Services.AddScoped<KhoaHocBLL>();
            builder.Services.AddScoped<BaiGiangBLL>();
            builder.Services.AddScoped<DangKyKhoaHocBLL>();
            builder.Services.AddScoped<BaiKiemTraBLL>();
            builder.Services.AddScoped<KetQuaKiemTraBLL>();
            builder.Services.AddScoped<ThongBaoBLL>();
            builder.Services.AddScoped<BaoCaoBLL>();
            builder.Services.AddSingleton<BLL.Export.IExportService>(BLL.Export.ExportService.Shared);

            // 6. Đăng ký Background Worker Queue (IHostedService)
            builder.Services.AddSingleton<IBackgroundTaskQueue>(BackgroundTaskQueue.Shared);
            builder.Services.AddHostedService<LmsBackgroundWorker>();

            // 7. Cấu hình Response Compression (Brotli & Gzip theo Bài 5 Giáo trình)
            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });
            builder.Services.Configure<BrotliCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest);
            builder.Services.Configure<GzipCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest);

            // 8. Cấu hình Rate Limiting (FixedWindowLimiter trả HTTP 429)
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = async (ctx, ct) =>
                {
                    ctx.HttpContext.Response.ContentType = "application/json; charset=utf-8";
                    await ctx.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(new
                    {
                        success = false,
                        statusCode = 429,
                        message = "Bạn đã gửi quá nhiều yêu cầu trong thời gian ngắn. Vui lòng thử lại sau ít giây.",
                        traceId = ctx.HttpContext.TraceIdentifier
                    }), ct);
                };

                options.AddFixedWindowLimiter("AuthPolicy", opt =>
                {
                    opt.PermitLimit = 30;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    opt.QueueLimit = 2;
                });

                options.AddFixedWindowLimiter("DefaultApiPolicy", opt =>
                {
                    opt.PermitLimit = 200;
                    opt.Window = TimeSpan.FromMinutes(1);
                });
            });

            // 9. Cấu hình Health Checks (/healthz, /health/live, /health/ready)
            builder.Services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy($"{serviceName} is live."), tags: new[] { "live" })
                .AddCheck<SqlServerConnectionHealthCheck>("sqlserver", tags: new[] { "ready" });

            return builder;
        }

        public static WebApplication UseLmsCorePipeline(this WebApplication app)
        {
            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<GlobalExceptionMiddleware>();
            app.UseResponseCompression();
            app.UseRateLimiter();

            app.MapHealthChecks("/healthz");
            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("ready")
            });

            return app;
        }
    }
}
