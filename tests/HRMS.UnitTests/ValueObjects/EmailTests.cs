using HRMS.Core.ValueObjects;

namespace HRMS.UnitTests.ValueObjects
{
    public class EmailTests
    {
        [Theory]
        [InlineData("user@example.com")]
        [InlineData("USER@EXAMPLE.COM")]
        [InlineData("user.name+tag@example.co.uk")]
        public void Create_WithValidEmail_ReturnsEmailObject(string input)
        {
            var email = Email.Create(input);

            Assert.NotNull(email);
            Assert.Equal(input.Trim().ToLowerInvariant(), email.Value);
        }

        [Theory]
        [InlineData("notanemail")]
        [InlineData("@missinglocal.com")]
        [InlineData("missingdomain@")]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithInvalidEmail_ThrowsArgumentException(string input)
        {
            Assert.Throws<ArgumentException>(() => Email.Create(input));
        }

        [Fact]
        public void Create_NormalisesToLowercase()
        {
            var email = Email.Create("User@Example.COM");

            Assert.Equal("user@example.com", email.Value);
        }

        [Fact]
        public void Equals_TwoEmailsWithSameValue_AreEqual()
        {
            var a = Email.Create("user@example.com");
            var b = Email.Create("USER@EXAMPLE.COM");

            Assert.Equal(a, b);
        }

        [Fact]
        public void Equals_TwoEmailsWithDifferentValues_AreNotEqual()
        {
            var a = Email.Create("alice@example.com");
            var b = Email.Create("bob@example.com");

            Assert.NotEqual(a, b);
        }

        [Fact]
        public void TryCreate_WithValidEmail_ReturnsTrueAndResult()
        {
            var succeeded = Email.TryCreate("user@example.com", out var email);

            Assert.True(succeeded);
            Assert.NotNull(email);
        }

        [Fact]
        public void TryCreate_WithInvalidEmail_ReturnsFalse()
        {
            var succeeded = Email.TryCreate("invalid-email", out var email);

            Assert.False(succeeded);
            Assert.Null(email);
        }

        [Fact]
        public void ImplicitConversion_ToStringReturnsValue()
        {
            var email = Email.Create("user@example.com");
            string value = email;

            Assert.Equal("user@example.com", value);
        }
    }
}
