namespace HospitalQueueSystem.Domain.Interfaces
{
    public interface IRedisService
    {
        Task SetAsync<T>(string key, T value);
        Task<T> GetAsync<T>(string key);
    }
}
