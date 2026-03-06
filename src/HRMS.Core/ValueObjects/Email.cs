using System.Text.RegularExpressions;

namespace HRMS.Core.ValueObjects
{
    /// <summary>
    /// Encapsulates an email address with format validation.
    /// Ensures that an email is always in a valid format wherever it is used.
    /// </summary>
    public sealed class Email : BaseValueObject
    {
        private static readonly Regex EmailRegex = new(
            @"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string Value { get; }

        private Email(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a new Email value object. Throws if the format is invalid.
        /// </summary>
        public static Email Create(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email address cannot be empty.", nameof(email));

            var normalised = email.Trim().ToLowerInvariant();

            if (!EmailRegex.IsMatch(normalised))
                throw new ArgumentException($"'{email}' is not a valid email address.", nameof(email));

            return new Email(normalised);
        }

        /// <summary>
        /// Tries to create an Email value object without throwing.
        /// </summary>
        public static bool TryCreate(string email, out Email? result)
        {
            try
            {
                result = Create(email);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value;

        public static implicit operator string(Email email) => email.Value;
    }
}
