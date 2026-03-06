namespace HRMS.Shared.Extensions
{
    /// <summary>
    /// Extension methods for string manipulation used across the application.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>Returns true if the string is null, empty, or consists only of whitespace.</summary>
        public static bool IsNullOrWhiteSpace(this string? value) => string.IsNullOrWhiteSpace(value);

        /// <summary>Truncates a string to the specified maximum length, appending "..." if truncated.</summary>
        public static string Truncate(this string value, int maxLength, string suffix = "...")
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value;

            return value[..(maxLength - suffix.Length)] + suffix;
        }

        /// <summary>Converts a string to title case (first letter of each word capitalised).</summary>
        public static string ToTitleCase(this string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLower());
        }

        /// <summary>Masks all but the last four characters of a sensitive string.</summary>
        public static string MaskSensitive(this string value, int visibleChars = 4, char maskChar = '*')
        {
            if (string.IsNullOrEmpty(value) || value.Length <= visibleChars)
                return value;

            return new string(maskChar, value.Length - visibleChars) + value[^visibleChars..];
        }
    }
}
