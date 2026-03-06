namespace HRMS.Core.ValueObjects
{
    /// <summary>
    /// Encapsulates a monetary value with currency code to prevent
    /// accidental mixing of different currencies and negative amounts.
    /// </summary>
    public sealed class Money : BaseValueObject
    {
        public decimal Amount { get; }
        public string Currency { get; }

        private Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        /// <summary>
        /// Creates a Money value object in the default currency (USD).
        /// </summary>
        public static Money Create(decimal amount, string currency = "USD")
        {
            if (amount < 0)
                throw new ArgumentException("Monetary amount cannot be negative.", nameof(amount));

            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException("Currency code cannot be empty.", nameof(currency));

            var normalisedCurrency = currency.Trim().ToUpperInvariant();
            if (normalisedCurrency.Length != 3)
                throw new ArgumentException("Currency code must be a 3-letter ISO 4217 code.", nameof(currency));

            return new Money(Math.Round(amount, 2), normalisedCurrency);
        }

        public Money Add(Money other)
        {
            if (other.Currency != Currency)
                throw new InvalidOperationException($"Cannot add amounts in different currencies ({Currency} and {other.Currency}).");
            return new Money(Amount + other.Amount, Currency);
        }

        public Money Subtract(Money other)
        {
            if (other.Currency != Currency)
                throw new InvalidOperationException($"Cannot subtract amounts in different currencies ({Currency} and {other.Currency}).");
            if (other.Amount > Amount)
                throw new InvalidOperationException("Resulting amount cannot be negative.");
            return new Money(Amount - other.Amount, Currency);
        }

        public Money Multiply(decimal factor)
        {
            if (factor < 0)
                throw new ArgumentException("Multiplication factor cannot be negative.", nameof(factor));
            return new Money(Math.Round(Amount * factor, 2), Currency);
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }

        public override string ToString() => $"{Currency} {Amount:N2}";

        public static implicit operator decimal(Money money) => money.Amount;
    }
}
