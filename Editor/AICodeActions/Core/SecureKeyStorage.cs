using System;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// Secure storage for API keys using encryption
    /// Keys are encrypted before storing in EditorPrefs
    /// </summary>
    public static class SecureKeyStorage
    {
        private const string ENTROPY_KEY = "AICodeActions_Entropy";
        private const string KEY_PREFIX = "AICodeActions_Secure_";

        // Machine-specific entropy for additional security
        private static byte[] GetEntropy()
        {
            string entropyBase = EditorPrefs.GetString(ENTROPY_KEY, "");

            if (string.IsNullOrEmpty(entropyBase))
            {
                // Generate new entropy on first use
                entropyBase = Guid.NewGuid().ToString() + Environment.MachineName;
                EditorPrefs.SetString(ENTROPY_KEY, entropyBase);
            }

            return Encoding.UTF8.GetBytes(entropyBase);
        }

        /// <summary>
        /// Store an API key securely
        /// </summary>
        public static void SetApiKey(string keyName, string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                EditorPrefs.DeleteKey(KEY_PREFIX + keyName);
                return;
            }

            try
            {
                string encrypted = Encrypt(apiKey);
                EditorPrefs.SetString(KEY_PREFIX + keyName, encrypted);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SecureKeyStorage] Encryption failed, using fallback: {ex.Message}");
                // Fallback to base64 encoding (not secure, but better than plain text)
                string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(apiKey));
                EditorPrefs.SetString(KEY_PREFIX + keyName, "B64:" + encoded);
            }
        }

        /// <summary>
        /// Retrieve an API key
        /// </summary>
        public static string GetApiKey(string keyName)
        {
            string stored = EditorPrefs.GetString(KEY_PREFIX + keyName, "");

            if (string.IsNullOrEmpty(stored))
            {
                // Check for legacy plain text key (migration)
                string legacyKey = EditorPrefs.GetString("AICodeActions_APIKey", "");
                if (!string.IsNullOrEmpty(legacyKey) && keyName == "APIKey")
                {
                    // Migrate to secure storage
                    SetApiKey(keyName, legacyKey);
                    EditorPrefs.DeleteKey("AICodeActions_APIKey");
                    return legacyKey;
                }
                return "";
            }

            try
            {
                // Check for base64 fallback
                if (stored.StartsWith("B64:"))
                {
                    string encoded = stored.Substring(4);
                    return Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                }

                return Decrypt(stored);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SecureKeyStorage] Decryption failed: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Check if a key exists
        /// </summary>
        public static bool HasApiKey(string keyName)
        {
            return EditorPrefs.HasKey(KEY_PREFIX + keyName) ||
                   (keyName == "APIKey" && EditorPrefs.HasKey("AICodeActions_APIKey"));
        }

        /// <summary>
        /// Delete a stored key
        /// </summary>
        public static void DeleteApiKey(string keyName)
        {
            EditorPrefs.DeleteKey(KEY_PREFIX + keyName);
        }

        /// <summary>
        /// Get masked version of API key for display
        /// </summary>
        public static string GetMaskedKey(string keyName)
        {
            string key = GetApiKey(keyName);
            if (string.IsNullOrEmpty(key))
                return "";

            if (key.Length <= 8)
                return new string('*', key.Length);

            return key.Substring(0, 4) + new string('*', key.Length - 8) + key.Substring(key.Length - 4);
        }

        private static string Encrypt(string plainText)
        {
            // Use AES encryption for all platforms (cross-platform compatible)
            return EncryptAES(plainText);
        }

        private static string Decrypt(string encryptedText)
        {
            // Use AES decryption for all platforms (cross-platform compatible)
            return DecryptAES(encryptedText);
        }

        // AES encryption for non-Windows platforms
        private static string EncryptAES(string plainText)
        {
            byte[] key = DeriveKey(GetEntropy(), 32);
            byte[] iv = DeriveKey(GetEntropy(), 16, "IV");

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                    byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                    return Convert.ToBase64String(encryptedBytes);
                }
            }
        }

        private static string DecryptAES(string encryptedText)
        {
            byte[] key = DeriveKey(GetEntropy(), 32);
            byte[] iv = DeriveKey(GetEntropy(), 16, "IV");

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                {
                    byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                    byte[] plainBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                    return Encoding.UTF8.GetString(plainBytes);
                }
            }
        }

        private static byte[] DeriveKey(byte[] entropy, int length, string salt = "KEY")
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(entropy, Encoding.UTF8.GetBytes(salt), 10000))
            {
                return deriveBytes.GetBytes(length);
            }
        }
    }
}
