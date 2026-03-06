using HRMS.Core.Interfaces.Services;
using HRMS.Shared.Constants;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace HRMS.Infrastructure.Services
{
    /// <summary>
    /// Service for encrypting and decrypting sensitive data using AES-256 encryption.
    /// </summary>
    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public EncryptionService(IConfiguration configuration)
        {
            // In production, these should come from Azure Key Vault or similar secure storage
            var keyString = configuration[HrmsConstants.Security.EncryptionKeyConfigName];
            var ivString = configuration[HrmsConstants.Security.EncryptionIVConfigName];

            if (string.IsNullOrEmpty(keyString) || string.IsNullOrEmpty(ivString))
            {
                throw new InvalidOperationException(
                    "Encryption key and IV must be configured in application settings. " +
                    "Use 'dotnet user-secrets set \"Encryption:Key\" \"<32-byte-base64-key>\"' " +
                    "and 'dotnet user-secrets set \"Encryption:IV\" \"<16-byte-base64-iv>\"' to configure.");
            }

            try
            {
                _key = Convert.FromBase64String(keyString);
                _iv = Convert.FromBase64String(ivString);

                if (_key.Length != 32) // AES-256 requires 32-byte key
                    throw new InvalidOperationException("Encryption key must be exactly 32 bytes (256 bits).");
                if (_iv.Length != 16) // AES requires 16-byte IV
                    throw new InvalidOperationException("Encryption IV must be exactly 16 bytes (128 bits).");
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(
                    "Encryption key and IV must be valid Base64 strings.", ex);
            }
        }

        /// <inheritdoc />
        public string? Encrypt(string? plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            
            return Convert.ToBase64String(cipherBytes);
        }

        /// <inheritdoc />
        public string? Decrypt(string? cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = _iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                var cipherBytes = Convert.FromBase64String(cipherText);
                var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (CryptographicException)
            {
                // Return null or empty if decryption fails (corrupted data)
                return null;
            }
            catch (FormatException)
            {
                // Return original value if it's not encrypted (backward compatibility)
                return cipherText;
            }
        }

        /// <inheritdoc />
        public string? Mask(string? value, int unmaskedPrefixLength = 2, int unmaskedSuffixLength = 2)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (value.Length <= unmaskedPrefixLength + unmaskedSuffixLength)
            {
                // If value is too short, mask everything except first character
                return value.Length > 1 
                    ? value[0] + new string(HrmsConstants.Security.MaskCharacter, value.Length - 1)
                    : new string(HrmsConstants.Security.MaskCharacter, value.Length);
            }

            var prefix = value.Substring(0, unmaskedPrefixLength);
            var suffix = value.Substring(value.Length - unmaskedSuffixLength);
            var maskedLength = value.Length - unmaskedPrefixLength - unmaskedSuffixLength;
            var masked = new string(HrmsConstants.Security.MaskCharacter, maskedLength);

            return $"{prefix}{masked}{suffix}";
        }
    }
}
