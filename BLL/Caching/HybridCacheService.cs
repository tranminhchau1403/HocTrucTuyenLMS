using System;
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace BLL.Caching
{
    public interface ICacheService
    {
        T? GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiration = null, TimeSpan? slidingExpiration = null);
        void Remove(string key);
        void RemoveByPrefix(string prefix);
    }

    /// <summary>
    /// Bộ đệm lai (Hybrid Cache-Aside Pattern) chuẩn Bài 11 Giáo trình:
    /// Ưu tiên kết nối Redis (IDistributedCache) nếu có cấu hình, tự động fallback sang IMemoryCache khi chạy độc lập
    /// và hỗ trợ xóa cache theo tiền tố (Prefix Invalidation) khi cập nhật khóa học/danh mục/báo cáo.
    /// </summary>
    public class HybridCacheService : ICacheService
    {
        public static readonly HybridCacheService Shared = new();

        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache? _distributedCache;
        private readonly ConcurrentDictionary<string, byte> _knownKeys = new(StringComparer.OrdinalIgnoreCase);

        public HybridCacheService(IMemoryCache? memoryCache = null, IDistributedCache? distributedCache = null)
        {
            _memoryCache = memoryCache ?? new MemoryCache(new MemoryCacheOptions());
            _distributedCache = distributedCache;
        }

        public T? GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiration = null, TimeSpan? slidingExpiration = null)
        {
            var ttl = expiration ?? TimeSpan.FromMinutes(15);
            var memOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            };
            if (slidingExpiration.HasValue)
            {
                memOptions.SlidingExpiration = slidingExpiration.Value;
            }

            // 1. Kiểm tra L1 In-Memory Cache
            if (_memoryCache.TryGetValue(key, out T? cachedValue) && cachedValue != null)
            {
                return cachedValue;
            }

            // 2. Kiểm tra L2 Redis Distributed Cache (nếu khả dụng)
            if (_distributedCache != null)
            {
                try
                {
                    string? json = _distributedCache.GetString(key);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var deserialized = JsonSerializer.Deserialize<T>(json);
                        if (deserialized != null)
                        {
                            _memoryCache.Set(key, deserialized, memOptions);
                            _knownKeys.TryAdd(key, 1);
                            return deserialized;
                        }
                    }
                }
                catch
                {
                    // Fallback an toàn về MemoryCache nếu Redis tạm thời ngắt kết nối
                }
            }

            // 3. Cache Miss -> Nạp từ DB qua factory
            T freshData = factory();
            if (freshData != null)
            {
                _memoryCache.Set(key, freshData, memOptions);
                _knownKeys.TryAdd(key, 1);

                if (_distributedCache != null)
                {
                    try
                    {
                        string serialized = JsonSerializer.Serialize(freshData);
                        var distOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = ttl
                        };
                        if (slidingExpiration.HasValue)
                        {
                            distOptions.SlidingExpiration = slidingExpiration.Value;
                        }
                        _distributedCache.SetString(key, serialized, distOptions);
                    }
                    catch
                    {
                        // Bỏ qua lỗi ghi Redis để không làm gián đoạn luồng chính
                    }
                }
            }

            return freshData;
        }

        public void Remove(string key)
        {
            _memoryCache.Remove(key);
            _knownKeys.TryRemove(key, out _);

            if (_distributedCache != null)
            {
                try
                {
                    _distributedCache.Remove(key);
                }
                catch
                {
                    // Ignore Redis disconnect
                }
            }
        }

        public void RemoveByPrefix(string prefix)
        {
            foreach (var key in _knownKeys.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    Remove(key);
                }
            }
        }
    }
}
