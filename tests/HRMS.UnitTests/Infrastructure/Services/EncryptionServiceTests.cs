using HRMS.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HRMS.UnitTests.Infrastructure.Services
{
    public class EncryptionServiceTests
    {
        private readonly EncryptionService _encryptionService;

        public EncryptionServiceTests()
        {
            // Create configuration with encryption keys
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "Encryption:Key", "w40hGa4On6BuQt3NL/NwMmZnzTXIwPv8HiZT/dPRgII=" },
                    { "Encryption:IV", "AabOjmbODg1xfqpnvgtJ/A==" }
                })
                .Build();

            _encryptionService = new EncryptionService(configuration);
        }

        [Fact]
        public void Encrypt_WhenGivenPlainText_ReturnsEncryptedString()
        {
            // Arrange
            var plainText = "123-45-6789";

            // Act
            var encrypted = _encryptionService.Encrypt(plainText);

            // Assert
            Assert.NotNull(encrypted);
            Assert.NotEqual(plainText, encrypted);
            Assert.True(encrypted.Length > 0);
        }

        [Fact]
        public void Decrypt_WhenGivenEncryptedText_ReturnsOriginalPlainText()
        {
            // Arrange
            var plainText = "123-45-6789";
            var encrypted = _encryptionService.Encrypt(plainText);

            // Act
            var decrypted = _encryptionService.Decrypt(encrypted);

            // Assert
            Assert.Equal(plainText, decrypted);
        }

        [Fact]
        public void Encrypt_Decrypt_RoundTrip_PreservesOriginalValue()
        {
            // Arrange
            var originalValue = "Passport-AB123456";

            // Act
            var encrypted = _encryptionService.Encrypt(originalValue);
            var decrypted = _encryptionService.Decrypt(encrypted);

            // Assert
            Assert.Equal(originalValue, decrypted);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Encrypt_WhenGivenNullOrEmpty_ReturnsInput(string? input)
        {
            // Act
            var result = _encryptionService.Encrypt(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Decrypt_WhenGivenNullOrEmpty_ReturnsInput(string? input)
        {
            // Act
            var result = _encryptionService.Decrypt(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Mask_WhenGivenValue_MasksMiddleCharacters()
        {
            // Arrange
            var value = "1234567890";

            // Act
            var masked = _encryptionService.Mask(value, 2, 2);

            // Assert
            Assert.Equal("12******90", masked);
        }

        [Fact]
        public void Mask_WhenValueTooShort_MasksAllButFirst()
        {
            // Arrange
            var value = "123";

            // Act
            var masked = _encryptionService.Mask(value, 2, 2);

            // Assert
            Assert.NotEqual(value, masked);
            Assert.StartsWith("1", masked);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Mask_WhenGivenNullOrEmpty_ReturnsInput(string? input)
        {
            // Act
            var result = _encryptionService.Mask(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Encrypt_ProducesDifferentOutputForDifferentInput()
        {
            // Arrange
            var plainText1 = "Value1";
            var plainText2 = "Value2";

            // Act
            var encrypted1 = _encryptionService.Encrypt(plainText1);
            var encrypted2 = _encryptionService.Encrypt(plainText2);

            // Assert
            Assert.NotEqual(encrypted1, encrypted2);
        }

        [Fact]
        public void Encrypt_ProducesConsistentOutput()
        {
            // Arrange
            var plainText = "ConsistentValue";

            // Act
            var encrypted1 = _encryptionService.Encrypt(plainText);
            var encrypted2 = _encryptionService.Encrypt(plainText);

            // Assert
            Assert.Equal(encrypted1, encrypted2);
        }

        [Fact]
        public void Decrypt_WithInvalidCipherText_ReturnsOriginalValue()
        {
            // Arrange
            var invalidCipherText = "ThisIsNotValidBase64!@#$%";

            // Act
            var result = _encryptionService.Decrypt(invalidCipherText);

            // Assert
            // Should return the original value for backward compatibility
            Assert.Equal(invalidCipherText, result);
        }

        [Fact]
        public void Mask_WithCustomParameters_MasksCorrectly()
        {
            // Arrange
            var value = "ABCDEFGHIJ";

            // Act
            var masked = _encryptionService.Mask(value, 3, 3);

            // Assert
            Assert.Equal("ABC****HIJ", masked);
        }
    }
}
