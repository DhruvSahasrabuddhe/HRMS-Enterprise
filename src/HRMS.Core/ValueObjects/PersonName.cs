namespace HRMS.Core.ValueObjects
{
    /// <summary>
    /// Encapsulates a person's name (first, optional middle, last)
    /// and provides consistent full-name formatting.
    /// </summary>
    public sealed class PersonName : BaseValueObject
    {
        public string FirstName { get; }
        public string? MiddleName { get; }
        public string LastName { get; }

        public string FullName => MiddleName is null
            ? $"{FirstName} {LastName}"
            : $"{FirstName} {MiddleName} {LastName}";

        public string DisplayName => $"{LastName}, {FirstName}";

        private PersonName(string firstName, string? middleName, string lastName)
        {
            FirstName = firstName;
            MiddleName = middleName;
            LastName = lastName;
        }

        /// <summary>
        /// Creates a PersonName value object. Throws if first or last name is missing.
        /// </summary>
        public static PersonName Create(string firstName, string lastName, string? middleName = null)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("First name cannot be empty.", nameof(firstName));

            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("Last name cannot be empty.", nameof(lastName));

            return new PersonName(
                firstName.Trim(),
                middleName?.Trim(),
                lastName.Trim());
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return FirstName.ToUpperInvariant();
            yield return MiddleName?.ToUpperInvariant();
            yield return LastName.ToUpperInvariant();
        }

        public override string ToString() => FullName;
    }
}
