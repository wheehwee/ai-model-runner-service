using Application.Interfaces;
using Common.Extensions;
using Domain.Entities;
using Domain.Enums;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Infrastructure.Data.Services.DataServices
{
    public abstract class ModelRunDataService : IModelRunDataService
    {
        protected static HashSet<string> baseStatusProperties = new HashSet<string>()
        {
            nameof(ModelRun.State)
        };

        public class CacheableEntry<T>
        {
            public T Value { get; set; }
            public DateTimeOffset? ExpireAt { get; set; }
        }

        private const string INCREMENTAL_ORDER_ID_HASH_KEY = "INCREMENTAL_ORDER_ID";

        private static SemaphoreSlim nextOrderCountLock = new SemaphoreSlim(1, 1);

        public abstract ModelRunType ModelRunType { get; }

        protected readonly IRedisDatabase _redisDatabase;
        protected DateTimeOffset GetExpireTime(string hashKey) 
        {
            var delimiterIndex = hashKey.LastIndexOf('_');
            var currentHashKeyDate = delimiterIndex >= 0 ? DateTimeOffset.Parse(hashKey.Substring(delimiterIndex + 1)).Date : TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, "Asia/Bangkok".ToTimeZoneInfo()).Date;

            return currentHashKeyDate.AddDays(1);
        }

        protected ModelRunDataService(IRedisDatabase redisDatabase)
        {
            _redisDatabase = redisDatabase;
        }

        protected async Task<T> HashWrapSetAsync<T>(string hashKey, string key, T item, DateTimeOffset? expireAt = null, bool setHashKeyToExpire = false)
        {
            await _redisDatabase.HashSetAsync(hashKey, key, new CacheableEntry<T>
            {
                Value = item,
                ExpireAt = expireAt
            });
            if (setHashKeyToExpire)
            {
                var delimiterIndex = hashKey.LastIndexOf('_');
                var currentHashKeyDate = delimiterIndex >= 0 ? DateTimeOffset.Parse(hashKey.Substring(delimiterIndex + 1)).Date : TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, "Asia/Bangkok".ToTimeZoneInfo()).Date;
                await _redisDatabase.UpdateExpiryAsync(hashKey, currentHashKeyDate.AddDays(1));
            }
            return item;
        }

        protected async Task<T> HashWrapGetAsync<T>(string hashKey, string key)
        {
            var result = await _redisDatabase.HashGetAsync<CacheableEntry<T>>(hashKey, key);

            return result == null ? default(T) : ((result.ExpireAt != null && result.ExpireAt <= TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, "Asia/Bangkok".ToTimeZoneInfo()) )? default(T) : result.Value);
        }

        public async Task<string> GetNextModelRunId()
        {
            var hashKey = $"{INCREMENTAL_ORDER_ID_HASH_KEY}_{TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, "Asia/Bangkok".ToTimeZoneInfo()).Date.ToString("yyyy-MM-dd")}";

            try
            {
                await nextOrderCountLock.WaitAsync();
                var currentRequestID = await HashWrapGetAsync<int?>(hashKey, ModelRunType.ToString());
                var nextRequestID = !currentRequestID.HasValue ? 0 : currentRequestID.Value + 1;
                await HashWrapSetAsync(hashKey, ModelRunType.ToString(), nextRequestID, GetExpireTime(hashKey), true);
                return $"{ModelRunType}_{TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, "Asia/Bangkok".ToTimeZoneInfo()).Date.ToString("yyyy-MM-dd")}_{nextRequestID}";
            }
            finally
            {
                nextOrderCountLock.Release();
            }
        }
    }
}
