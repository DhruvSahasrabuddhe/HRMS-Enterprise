using HRMS.Core.ValueObjects;

namespace HRMS.UnitTests.ValueObjects
{
    public class MoneyTests
    {
        [Fact]
        public void Create_WithValidAmount_ReturnsMoneyObject()
        {
            var money = Money.Create(100.50m, "USD");

            Assert.Equal(100.50m, money.Amount);
            Assert.Equal("USD", money.Currency);
        }

        [Fact]
        public void Create_WithNegativeAmount_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Money.Create(-1m, "USD"));
        }

        [Fact]
        public void Create_WithInvalidCurrencyCode_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Money.Create(100m, "US")); // Only 2 chars
            Assert.Throws<ArgumentException>(() => Money.Create(100m, ""));
        }

        [Fact]
        public void Create_NormalisesToUppercase()
        {
            var money = Money.Create(100m, "usd");

            Assert.Equal("USD", money.Currency);
        }

        [Fact]
        public void Add_TwoAmountsInSameCurrency_ReturnsSum()
        {
            var a = Money.Create(100m, "USD");
            var b = Money.Create(50m, "USD");

            var result = a.Add(b);

            Assert.Equal(150m, result.Amount);
        }

        [Fact]
        public void Add_DifferentCurrencies_ThrowsInvalidOperationException()
        {
            var usd = Money.Create(100m, "USD");
            var eur = Money.Create(50m, "EUR");

            Assert.Throws<InvalidOperationException>(() => usd.Add(eur));
        }

        [Fact]
        public void Subtract_ValidAmount_ReturnsDifference()
        {
            var a = Money.Create(100m, "USD");
            var b = Money.Create(40m, "USD");

            var result = a.Subtract(b);

            Assert.Equal(60m, result.Amount);
        }

        [Fact]
        public void Subtract_ExceedingAmount_ThrowsInvalidOperationException()
        {
            var a = Money.Create(10m, "USD");
            var b = Money.Create(20m, "USD");

            Assert.Throws<InvalidOperationException>(() => a.Subtract(b));
        }

        [Fact]
        public void Multiply_ByFactor_ReturnsScaledAmount()
        {
            var salary = Money.Create(1000m, "USD");

            var result = salary.Multiply(1.1m);

            Assert.Equal(1100m, result.Amount);
        }

        [Fact]
        public void Equals_SameAmountAndCurrency_AreEqual()
        {
            var a = Money.Create(500m, "USD");
            var b = Money.Create(500m, "USD");

            Assert.Equal(a, b);
        }

        [Fact]
        public void ImplicitConversion_ToDecimalReturnsAmount()
        {
            var money = Money.Create(250.75m, "USD");
            decimal value = money;

            Assert.Equal(250.75m, value);
        }
    }
}
