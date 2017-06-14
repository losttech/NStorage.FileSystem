namespace LostTech.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
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
            key = KeyEncode(key);
            // encode final directory separator, if any
            char lastChar = key[key.Length - 1];
            if (Path.DirectorySeparatorChar == lastChar || Path.AltDirectorySeparatorChar == lastChar)
                key = key.Substring(0, key.Length - 1) + $"%{(byte)lastChar:x2}";
            // TODO: SECURITY: ensure key points within directory
            return Path.Combine(this.directory.FullName, key);
        }

        public Task<bool?> Delete(string key)
        {
            string fullPath = this.GetFullPath(key);
            bool existed = File.Exists(fullPath);
            if (existed)
                File.Delete(this.GetFullPath(key));
            return Task.FromResult((bool?)existed);
        }

        public Task<byte[]> Get(string key)
        {
            string fullPath = this.GetFullPath(key);
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
            string fullPath = this.GetFullPath(key);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllBytes(fullPath, value);
            return Task.CompletedTask;
        }

        public Task<(bool, byte[])> TryGet(string key)
        {
            string fullPath = this.GetFullPath(key);
            if (!File.Exists(fullPath))
                return Task.FromResult((false, default(byte[])));
            try {
                return Task.FromResult((true, File.ReadAllBytes(fullPath)));
            } catch (FileNotFoundException) {
                return Task.FromResult((false, default(byte[])));
            }
        }

        static readonly Regex UnsupportedKeyCharRegex = new Regex(
            $"[{string.Join("", Path.GetInvalidPathChars().Concat(new []{'%'}).Select(c => "\\"+c))}]",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        static readonly Regex UnsupportedEscapedKeyCharRegex = new Regex(@"\%[a-f0-9]{2}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public static string KeyEncode(string key) =>
            string.IsNullOrEmpty(key)
                ? throw new ArgumentNullException(nameof(key))
                : UnsupportedKeyCharRegex.Replace(key, badMatch => $"%{(byte)badMatch.Value[0]:x2}");
        public static string KeyDecode(string key) =>
            string.IsNullOrEmpty(key)
                ? throw new ArgumentNullException(nameof(key))
                : UnsupportedEscapedKeyCharRegex.Replace(key, badMatch => ((char)Byte.Parse(badMatch.Value.Substring(1), NumberStyles.HexNumber)).ToString());
    }
}
