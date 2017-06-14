namespace LostTech.Storage
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    public sealed class DirectoryStorage : IWriteableKeyValueStore<string, byte[]>
    {
        readonly DirectoryInfo directory;
        public DirectoryStorage(DirectoryInfo directory)
        {
            this.directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        string GetFullPath(string key)
        {
            // TODO: SECURITY: ensure key points within directory
            return Path.Combine(this.directory.FullName, key);
        }

        public Task<bool?> Delete(string key)
        {
            string fullPath = GetFullPath(key);
            bool existed = File.Exists(fullPath);
            if (existed)
                File.Delete(GetFullPath(key));
            return Task.FromResult((bool?)existed);
        }

        public Task<byte[]> Get(string key)
        {
            string fullPath = GetFullPath(key);
            try
            {
                return Task.FromResult(File.ReadAllBytes(fullPath));
            }
            catch (FileNotFoundException e)
            {
                throw new KeyNotFoundException("File not found", innerException: e);
            }
        }

        public Task Put(string key, byte[] value)
        {
            string fullPath = GetFullPath(key);
            File.WriteAllBytes(fullPath, value);
            return Task.CompletedTask;
        }

        public Task<(bool, byte[])> TryGet(string key)
        {
            string fullPath = GetFullPath(key);
            if (!File.Exists(fullPath))
                return Task.FromResult((false, default(byte[])));
            try {
                return Task.FromResult((true, File.ReadAllBytes(fullPath)));
            } catch (FileNotFoundException e) {
                return Task.FromResult((false, default(byte[])));
            }
        }
    }
}
