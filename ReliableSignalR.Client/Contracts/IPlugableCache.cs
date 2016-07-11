namespace ReliableSignalR.Client.Contracts
{
    public interface IPlugableCache
    {
        void Save(string key, string value);
        string Get(string key);
        void Delete(string key);
        void Flush();
    }
}
