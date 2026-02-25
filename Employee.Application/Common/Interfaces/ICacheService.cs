namespace Employee.Application.Common.Interfaces
{
    public interface ICacheService
    {
        /// <summary>
        /// Lấy dữ liệu từ Cache theo Key.
        /// </summary>
        Task<T?> GetAsync<T>(string key);

        /// <summary>
        /// Lưu dữ liệu vào Cache.
        /// </summary>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

        /// <summary>
        /// Xóa dữ liệu khỏi Cache.
        /// </summary>
        Task RemoveAsync(string key);
    }
}
