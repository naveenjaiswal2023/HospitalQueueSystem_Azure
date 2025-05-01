using HospitalQueueSystem.Domain.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace HospitalQueueSystem.Application.Services
{
    public class RedisService : IRedisService
    {
        private readonly IConnectionMultiplexer _redis;

        public RedisService(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        // Asynchronously set a value in Redis
        public async Task SetAsync<T>(string key, T value)
        {
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(value);
            await db.StringSetAsync(key, json);
        }

        // Asynchronously get a value from Redis
        public async Task<T> GetAsync<T>(string key)
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(key);

            if (value.IsNullOrEmpty) return default;

            return JsonSerializer.Deserialize<T>(value);
        }
    }
}
