using HRMS.Core.Helpers;
using Xunit;

namespace HRMS.UnitTests.Core.Helpers
{
    public class FileUploadValidatorTests
    {
        [Theory]
        [InlineData("document.pdf", 1024, "application/pdf", true)]
        [InlineData("spreadsheet.xlsx", 1024, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", true)]
        [InlineData("text.txt", 1024, "text/plain", true)]
        public void ValidateDocument_WithValidFile_ReturnsTrue(string fileName, long fileSize, string contentType, bool expectedResult)
        {
            // Act
            var (isValid, errorMessage) = FileUploadValidator.ValidateDocument(fileName, fileSize, contentType);

            // Assert
            Assert.Equal(expectedResult, isValid);
            Assert.Null(errorMessage);
        }

        [Theory]
        [InlineData("malicious.exe", 1024, "application/x-msdownload", false)]
        [InlineData("script.js", 1024, "application/javascript", false)]
        [InlineData("image.png", 1024, "image/png", false)]
        public void ValidateDocument_WithInvalidFileType_ReturnsFalse(string fileName, long fileSize, string contentType, bool expectedResult)
        {
            // Act
            var (isValid, errorMessage) = FileUploadValidator.ValidateDocument(fileName, fileSize, contentType);

            // Assert
            Assert.Equal(expectedResult, isValid);
            Assert.NotNull(errorMessage);
        }

        [Fact]
        public void ValidateDocument_WithOversizedFile_ReturnsFalse()
        {
            // Arrange
            var fileName = "large.pdf";
            var fileSize = 11 * 1024 * 1024; // 11 MB (over limit)
            var contentType = "application/pdf";

            // Act
            var (isValid, errorMessage) = FileUploadValidator.ValidateDocument(fileName, fileSize, contentType);

            // Assert
            Assert.False(isValid);
            Assert.Contains("cannot exceed", errorMessage);
        }

        [Theory]
        [InlineData("../../../etc/passwd")]
        [InlineData("..\\..\\windows\\system32\\config")]
        [InlineData("subdir/file.pdf")]
        public void ValidateDocument_WithPathTraversal_ReturnsFalse(string fileName)
        {
            // Arrange
            var fileSize = 1024;
            var contentType = "application/pdf";

            // Act
            var (isValid, errorMessage) = FileUploadValidator.ValidateDocument(fileName, fileSize, contentType);

            // Assert
            Assert.False(isValid);
            Assert.Contains("path traversal", errorMessage);
        }

        [Theory]
        [InlineData("image.jpg", 1024, "image/jpeg", true)]
        [InlineData("photo.png", 1024, "image/png", true)]
        [InlineData("graphic.gif", 1024, "image/gif", true)]
        public void ValidateImage_WithValidFile_ReturnsTrue(string fileName, long fileSize, string contentType, bool expectedResult)
        {
            // Act
            var (isValid, errorMessage) = FileUploadValidator.ValidateImage(fileName, fileSize, contentType);

            // Assert
            Assert.Equal(expectedResult, isValid);
            Assert.Null(errorMessage);
        }

        [Theory]
        [InlineData("document.pdf", 1024, "application/pdf", false)]
        [InlineData("script.exe", 1024, "application/x-msdownload", false)]
        public void ValidateImage_WithNonImageFile_ReturnsFalse(string fileName, long fileSize, string contentType, bool expectedResult)
        {
            // Act
            var (isValid, errorMessage) = FileUploadValidator.ValidateImage(fileName, fileSize, contentType);

            // Assert
            Assert.Equal(expectedResult, isValid);
            Assert.NotNull(errorMessage);
        }

        [Theory]
        [InlineData("my file.pdf", "my_file.pdf")]
        [InlineData("file@#$%.txt", "file.txt")]
        [InlineData("normal.doc", "normal.doc")]
        public void SanitizeFileName_RemovesInvalidCharacters(string input, string expected)
        {
            // Act
            var result = FileUploadValidator.SanitizeFileName(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void SanitizeFileName_WithEmptyInput_ReturnsGuid(string? input)
        {
            // Act
            var result = FileUploadValidator.SanitizeFileName(input!);

            // Assert
            Assert.StartsWith("file_", result);
            Assert.True(result.Length > 10);
        }

        [Fact]
        public void GenerateUniqueFileName_CreatesUniqueNames()
        {
            // Arrange
            var fileName = "document.pdf";

            // Act
            var result1 = FileUploadValidator.GenerateUniqueFileName(fileName);
            var result2 = FileUploadValidator.GenerateUniqueFileName(fileName);

            // Assert
            Assert.NotEqual(result1, result2);
            Assert.EndsWith(".pdf", result1);
            Assert.EndsWith(".pdf", result2);
            Assert.Contains("document_", result1);
            Assert.Contains("document_", result2);
        }

        [Fact]
        public void GenerateUniqueFileName_PreservesExtension()
        {
            // Arrange
            var fileName = "myfile.xlsx";

            // Act
            var result = FileUploadValidator.GenerateUniqueFileName(fileName);

            // Assert
            Assert.EndsWith(".xlsx", result);
        }

        [Fact]
        public void ValidateDocument_WithEmptyFileName_ReturnsFalse()
        {
            // Act
            var (isValid, errorMessage) = FileUploadValidator.ValidateDocument("", 1024, "application/pdf");

            // Assert
            Assert.False(isValid);
            Assert.Equal("File name is required", errorMessage);
        }

        [Fact]
        public void ValidateDocument_WithZeroFileSize_ReturnsFalse()
        {
            // Act
            var (isValid, errorMessage) = FileUploadValidator.ValidateDocument("file.pdf", 0, "application/pdf");

            // Assert
            Assert.False(isValid);
            Assert.Contains("must be greater than 0", errorMessage);
        }

        [Fact]
        public void ValidateDocument_WithWrongMimeType_ReturnsFalse()
        {
            // Act
            var (isValid, errorMessage) = FileUploadValidator.ValidateDocument("file.pdf", 1024, "image/jpeg");

            // Assert
            Assert.False(isValid);
            Assert.Contains("content type", errorMessage);
        }
    }
}
