using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CtYun.Services
{
    public class ConfigService
    {
        private static readonly Lazy<ConfigService> _instance = new(() => new ConfigService());
        public static ConfigService Instance => _instance.Value;

        private readonly string _configFilePath;
        private readonly string _keyFilePath;
        private readonly byte[] _key;

        private ConfigService()
        {
            var configDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
            Directory.CreateDirectory(configDir);
            _configFilePath = Path.Combine(configDir, "user.config");
            _keyFilePath = Path.Combine(configDir, "key.bin");
            _key = GetOrCreateKey();
        }

        private byte[] GetOrCreateKey()
        {
            if (File.Exists(_keyFilePath))
            {
                return File.ReadAllBytes(_keyFilePath);
            }

            var key = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            File.WriteAllBytes(_keyFilePath, key);
            return key;
        }

        public void SaveCredentials(string phone, string password)
        {
            try
            {
                var config = new UserConfig
                {
                    Phone = Encrypt(phone),
                    Password = Encrypt(password),
                    SavedAt = DateTime.Now
                };

                var json = JsonSerializer.Serialize(config);
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                Logger.Instance.Log($"保存凭据失败: {ex.Message}", LogLevel.Error);
            }
        }

        public (string Phone, string Password) LoadCredentials()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                    return (null, null);

                var json = File.ReadAllText(_configFilePath);
                var config = JsonSerializer.Deserialize<UserConfig>(json);

                if (config == null)
                    return (null, null);

                return (Decrypt(config.Phone), Decrypt(config.Password));
            }
            catch (Exception ex)
            {
                Logger.Instance.Log($"加载凭据失败: {ex.Message}", LogLevel.Error);
                return (null, null);
            }
        }

        public void ClearCredentials()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    File.Delete(_configFilePath);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log($"清除凭据失败: {ex.Message}", LogLevel.Error);
            }
        }

        private string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return null;

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var result = new byte[aes.IV.Length + encryptedBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

            return Convert.ToBase64String(result);
        }

        private string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return null;

            try
            {
                var fullCipher = Convert.FromBase64String(cipherText);
                using var aes = Aes.Create();
                aes.Key = _key;

                var iv = new byte[16];
                var cipher = new byte[fullCipher.Length - 16];
                Buffer.BlockCopy(fullCipher, 0, iv, 0, 16);
                Buffer.BlockCopy(fullCipher, 16, cipher, 0, cipher.Length);
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return null;
            }
        }
    }

    public class UserConfig
    {
        public string Phone { get; set; }
        public string Password { get; set; }
        public DateTime SavedAt { get; set; }
    }
}
