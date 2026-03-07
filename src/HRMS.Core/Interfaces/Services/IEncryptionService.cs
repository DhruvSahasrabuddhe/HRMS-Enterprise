namespace HRMS.Core.Interfaces.Services
{
    /// <summary>
    /// Service for encrypting and decrypting sensitive data.
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>
        /// Encrypts a plaintext string.
        /// </summary>
        /// <param name="plainText">The plaintext to encrypt.</param>
        /// <returns>The encrypted string in Base64 format, or null if input is null/empty.</returns>
        string? Encrypt(string? plainText);

        /// <summary>
        /// Decrypts an encrypted string.
        /// </summary>
        /// <param name="cipherText">The encrypted string in Base64 format.</param>
        /// <returns>The decrypted plaintext, or null if input is null/empty.</returns>
        string? Decrypt(string? cipherText);

        /// <summary>
        /// Masks a sensitive string for display purposes.
        /// </summary>
        /// <param name="value">The value to mask.</param>
        /// <param name="unmaskedPrefixLength">Number of characters to show at the start.</param>
        /// <param name="unmaskedSuffixLength">Number of characters to show at the end.</param>
        /// <returns>The masked string, or null if input is null/empty.</returns>
        string? Mask(string? value, int unmaskedPrefixLength = 2, int unmaskedSuffixLength = 2);
    }
}
