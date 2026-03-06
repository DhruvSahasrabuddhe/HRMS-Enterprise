using HRMS.Shared.Common;

namespace HRMS.UnitTests.Common
{
    public class ResultTests
    {
        [Fact]
        public void Success_ReturnsSuccessResult()
        {
            var result = Result.Success();

            Assert.True(result.IsSuccess);
            Assert.False(result.IsFailure);
            Assert.Null(result.Error);
        }

        [Fact]
        public void Failure_ReturnsFailureResult()
        {
            var result = Result.Failure("Something went wrong");

            Assert.False(result.IsSuccess);
            Assert.True(result.IsFailure);
            Assert.Equal("Something went wrong", result.Error);
        }

        [Fact]
        public void SuccessOfT_ReturnsSuccessResultWithValue()
        {
            var result = Result.Success(42);

            Assert.True(result.IsSuccess);
            Assert.Equal(42, result.Value);
        }

        [Fact]
        public void FailureOfT_ReturnsFailureResult()
        {
            var result = Result.Failure<int>("Not found");

            Assert.False(result.IsSuccess);
            Assert.Equal("Not found", result.Error);
        }

        [Fact]
        public void Value_OnFailureResult_ThrowsInvalidOperationException()
        {
            var result = Result.Failure<string>("error");

            Assert.Throws<InvalidOperationException>(() => _ = result.Value);
        }

        [Fact]
        public void NotFound_ReturnsFailureWithExpectedMessage()
        {
            var result = Result.NotFound<string>("Employee", 42);

            Assert.False(result.IsSuccess);
            Assert.Contains("Employee", result.Error);
            Assert.Contains("42", result.Error);
        }
    }
}
