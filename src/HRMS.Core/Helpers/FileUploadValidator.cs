using HRMS.Shared.Constants;

namespace HRMS.Core.Helpers
{
    /// <summary>
    /// Helper class for validating file uploads to prevent security vulnerabilities.
    /// </summary>
    public static class FileUploadValidator
    {
        // Maximum file size: 10 MB
        private const long MaxFileSizeBytes = 10 * 1024 * 1024;

        // Allowed file extensions for document uploads
        private static readonly HashSet<string> AllowedDocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv"
        };

        // Allowed file extensions for image uploads
        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp"
        };

        // Allowed MIME types for documents
        private static readonly HashSet<string> AllowedDocumentMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "text/plain",
            "text/csv"
        };

        // Allowed MIME types for images
        private static readonly HashSet<string> AllowedImageMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/gif",
            "image/bmp"
        };

        /// <summary>
        /// Validates a file upload for documents.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="fileSize">The size of the file in bytes.</param>
        /// <param name="contentType">The MIME type of the file.</param>
        /// <returns>A tuple indicating if validation passed and an error message if it failed.</returns>
        public static (bool IsValid, string? ErrorMessage) ValidateDocument(string fileName, long fileSize, string? contentType)
        {
            return ValidateFile(fileName, fileSize, contentType, AllowedDocumentExtensions, AllowedDocumentMimeTypes);
        }

        /// <summary>
        /// Validates a file upload for images.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="fileSize">The size of the file in bytes.</param>
        /// <param name="contentType">The MIME type of the file.</param>
        /// <returns>A tuple indicating if validation passed and an error message if it failed.</returns>
        public static (bool IsValid, string? ErrorMessage) ValidateImage(string fileName, long fileSize, string? contentType)
        {
            return ValidateFile(fileName, fileSize, contentType, AllowedImageExtensions, AllowedImageMimeTypes);
        }

        /// <summary>
        /// Validates a file upload against specified constraints.
        /// </summary>
        private static (bool IsValid, string? ErrorMessage) ValidateFile(
            string fileName,
            long fileSize,
            string? contentType,
            HashSet<string> allowedExtensions,
            HashSet<string> allowedMimeTypes)
        {
            // Validate file name
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return (false, "File name is required");
            }

            // Check for path traversal attempts
            if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
            {
                return (false, "Invalid file name - path traversal detected");
            }

            // Validate file size
            if (fileSize <= 0)
            {
                return (false, "File size must be greater than 0");
            }

            if (fileSize > MaxFileSizeBytes)
            {
                return (false, $"File size cannot exceed {MaxFileSizeBytes / (1024 * 1024)} MB");
            }

            // Validate file extension
            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
            {
                var allowedExtensionsString = string.Join(", ", allowedExtensions);
                return (false, $"File type not allowed. Allowed types: {allowedExtensionsString}");
            }

            // Validate MIME type
            if (string.IsNullOrWhiteSpace(contentType) || !allowedMimeTypes.Contains(contentType))
            {
                return (false, "Invalid or unsupported file content type");
            }

            return (true, null);
        }

        /// <summary>
        /// Generates a safe file name by removing potentially dangerous characters.
        /// </summary>
        /// <param name="fileName">The original file name.</param>
        /// <returns>A sanitized file name.</returns>
        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return $"file_{Guid.NewGuid():N}";
            }

            // Remove path components
            fileName = Path.GetFileName(fileName);

            // Remove invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());

            // Also remove potentially problematic characters
            var problematicChars = new[] { '@', '#', '$', '%', '&', '!', '~', '`' };
            sanitized = new string(sanitized.Where(c => !problematicChars.Contains(c)).ToArray());

            // Remove spaces
            sanitized = sanitized.Replace(" ", "_");

            // Ensure we have something left
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                return $"file_{Guid.NewGuid():N}";
            }

            return sanitized;
        }

        /// <summary>
        /// Generates a unique file name to prevent collisions and overwriting.
        /// </summary>
        /// <param name="originalFileName">The original file name.</param>
        /// <returns>A unique file name with timestamp and GUID.</returns>
        public static string GenerateUniqueFileName(string originalFileName)
        {
            var sanitizedName = SanitizeFileName(originalFileName);
            var extension = Path.GetExtension(sanitizedName);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitizedName);
            
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var uniqueId = Guid.NewGuid().ToString("N").Substring(0, HrmsConstants.Validation.UniqueFileNameGuidLength);

            return $"{nameWithoutExtension}_{timestamp}_{uniqueId}{extension}";
        }
    }
}
