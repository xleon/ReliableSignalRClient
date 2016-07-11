using Newtonsoft.Json;
using ReliableSignalR.Client.Contracts;

namespace ReliableSignalR.Client.Utils
{
    public class PlugableCacheWrapper
    {
        private readonly IPlugableCache _plugableCache;

        public PlugableCacheWrapper(IPlugableCache plugableCache)
        {
            _plugableCache = plugableCache;
        }

        public void Save<T>(string key, T value)
        {
            var serialized = JsonConvert.SerializeObject(value);
            _plugableCache.Save(key, serialized);
        }

        public T Get<T>(string key)
        {
            var serialized = _plugableCache.Get(key);
            return JsonConvert.DeserializeObject<T>(serialized);
        }

        public void Delete(string key) => _plugableCache.Delete(key);
        public void Flush() => _plugableCache.Flush();
    }
}
