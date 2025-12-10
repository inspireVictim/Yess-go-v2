using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace YessGoFront.Services
{
    public class SecureStorageService
    {
        public async Task SaveAsync(string key, string value)
        {
            await SecureStorage.SetAsync(key, value);
        }

        [Obsolete("Use GetAsync instead to avoid blocking UI thread")]
        public string? Get(string key)
        {
            // Предупреждение: этот метод блокирует UI поток
            // Используйте GetAsync вместо этого
            return SecureStorage.GetAsync(key).GetAwaiter().GetResult();
        }

        public async Task<string?> GetAsync(string key)
        {
            return await SecureStorage.GetAsync(key);
        }

        [Obsolete("Use HasAsync instead to avoid blocking UI thread")]
        public bool Has(string key)
        {
            // Предупреждение: этот метод блокирует UI поток
            // Используйте HasAsync вместо этого
            var val = SecureStorage.GetAsync(key).GetAwaiter().GetResult();
            return !string.IsNullOrEmpty(val);
        }

        public async Task<bool> HasAsync(string key)
        {
            var val = await SecureStorage.GetAsync(key);
            return !string.IsNullOrEmpty(val);
        }

        public async Task RemoveAsync(string key)
        {
            SecureStorage.Remove(key);
            await Task.CompletedTask;
        }

        public void Remove(string key)
        {
            SecureStorage.Remove(key);
        }

        public void ClearAll()
        {
            SecureStorage.RemoveAll();
        }
    }
}
