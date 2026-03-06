using HRMS.Core.ValueObjects;

namespace HRMS.UnitTests.ValueObjects
{
    public class PersonNameTests
    {
        [Fact]
        public void Create_WithValidNames_ReturnsPersonNameObject()
        {
            var name = PersonName.Create("John", "Doe");

            Assert.Equal("John", name.FirstName);
            Assert.Equal("Doe", name.LastName);
            Assert.Null(name.MiddleName);
        }

        [Fact]
        public void Create_WithMiddleName_IncludesMiddleName()
        {
            var name = PersonName.Create("John", "Doe", "Michael");

            Assert.Equal("Michael", name.MiddleName);
        }

        [Fact]
        public void FullName_WithoutMiddleName_ReturnsFirstLast()
        {
            var name = PersonName.Create("Jane", "Smith");

            Assert.Equal("Jane Smith", name.FullName);
        }

        [Fact]
        public void FullName_WithMiddleName_ReturnsFirstMiddleLast()
        {
            var name = PersonName.Create("Jane", "Smith", "Anne");

            Assert.Equal("Jane Anne Smith", name.FullName);
        }

        [Fact]
        public void DisplayName_ReturnsLastFirst()
        {
            var name = PersonName.Create("John", "Doe");

            Assert.Equal("Doe, John", name.DisplayName);
        }

        [Fact]
        public void Create_WithEmptyFirstName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => PersonName.Create("", "Doe"));
        }

        [Fact]
        public void Create_WithEmptyLastName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => PersonName.Create("John", ""));
        }

        [Fact]
        public void Equals_SameNames_AreEqual()
        {
            var a = PersonName.Create("John", "Doe");
            var b = PersonName.Create("john", "doe");

            Assert.Equal(a, b);
        }

        [Fact]
        public void Equals_DifferentNames_AreNotEqual()
        {
            var a = PersonName.Create("Alice", "Smith");
            var b = PersonName.Create("Bob", "Smith");

            Assert.NotEqual(a, b);
        }

        [Fact]
        public void Create_TrimsWhitespace()
        {
            var name = PersonName.Create("  John  ", "  Doe  ");

            Assert.Equal("John", name.FirstName);
            Assert.Equal("Doe", name.LastName);
        }
    }
}
