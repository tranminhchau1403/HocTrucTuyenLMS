using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Models
{
    public class PaginationMeta
    {
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }

    /// <summary>
    /// Chuẩn ApiResponse thống nhất theo Phụ lục A Giáo trình (kết hợp RFC 7807 ProblemDetails khi lỗi)
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Errors { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PaginationMeta? PaginationMeta { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TraceId { get; set; }

        public static ApiResponse<T> Ok(T? data, string message = "Thành công.", PaginationMeta? pagination = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                StatusCode = 200,
                Message = message,
                Data = data,
                PaginationMeta = pagination
            };
        }

        public static ApiResponse<T> Created(T? data, string message = "Tạo mới thành công.")
        {
            return new ApiResponse<T>
            {
                Success = true,
                StatusCode = 201,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> Fail(int statusCode, string message, object? errors = null, string? traceId = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                StatusCode = statusCode,
                Message = message,
                Errors = errors,
                TraceId = traceId
            };
        }
    }

    /// <summary>
    /// Mẫu Result&lt;T&gt; chuẩn Bài 6 Giáo trình dùng cho tầng Business Logic (BLL)
    /// </summary>
    public class Result<T>
    {
        public bool IsSuccess { get; init; }
        public int StatusCode { get; init; }
        public string Message { get; init; } = string.Empty;
        public T? Data { get; init; }
        public IDictionary<string, string[]>? ValidationErrors { get; init; }

        public static Result<T> Success(T data, string message = "Thành công.", int statusCode = 200)
            => new() { IsSuccess = true, StatusCode = statusCode, Message = message, Data = data };

        public static Result<T> Failure(string message, int statusCode = 400, IDictionary<string, string[]>? errors = null)
            => new() { IsSuccess = false, StatusCode = statusCode, Message = message, ValidationErrors = errors };
    }

    // Hệ thống Domain Exceptions chuẩn hóa HTTP Status Codes (400, 401, 403, 404, 409, 422)
    public abstract class AppException : Exception
    {
        public int StatusCode { get; }
        public object? Errors { get; }

        protected AppException(string message, int statusCode, object? errors = null) : base(message)
        {
            StatusCode = statusCode;
            Errors = errors;
        }
    }

    public class BadRequestException : AppException
    {
        public BadRequestException(string message, object? errors = null) : base(message, 400, errors) { }
    }

    public class UnauthorizedAppException : AppException
    {
        public UnauthorizedAppException(string message = "Thông tin xác thực không hợp lệ hoặc đã hết hạn.") : base(message, 401) { }
    }

    public class ForbiddenAppException : AppException
    {
        public ForbiddenAppException(string message = "Bạn không có quyền truy cập tài nguyên này.") : base(message, 403) { }
    }

    public class NotFoundAppException : AppException
    {
        public NotFoundAppException(string message = "Không tìm thấy tài nguyên yêu cầu.") : base(message, 404) { }
    }

    public class ConflictAppException : AppException
    {
        public ConflictAppException(string message) : base(message, 409) { }
    }

    public class UnprocessableEntityAppException : AppException
    {
        public UnprocessableEntityAppException(string message, object? errors = null) : base(message, 422, errors) { }
    }
}
