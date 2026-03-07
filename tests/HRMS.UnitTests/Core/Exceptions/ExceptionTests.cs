using HRMS.Core.Exceptions;

namespace HRMS.UnitTests.Core.Exceptions
{
    public class ExceptionTests
    {
        // ─── HrmsException (via concrete subtype) ────────────────────────────────

        [Fact]
        public void BusinessException_HasExpectedMessageAndDefaultErrorCode()
        {
            var ex = new BusinessException("Salary cannot be negative.");

            Assert.Equal("Salary cannot be negative.", ex.Message);
            Assert.Equal("BUSINESS_RULE_VIOLATION", ex.ErrorCode);
        }

        [Fact]
        public void BusinessException_WithCustomErrorCode_StoresErrorCode()
        {
            var ex = new BusinessException("Duplicate email.", "DUPLICATE_EMAIL");

            Assert.Equal("DUPLICATE_EMAIL", ex.ErrorCode);
            Assert.Equal("Duplicate email.", ex.Message);
        }

        [Fact]
        public void BusinessException_WithInnerException_PreservesInnerException()
        {
            var inner = new InvalidOperationException("inner");
            var ex = new BusinessException("Outer message.", "OUTER_CODE", inner);

            Assert.Same(inner, ex.InnerException);
            Assert.Equal("OUTER_CODE", ex.ErrorCode);
        }

        [Fact]
        public void BusinessException_IsHrmsException()
        {
            var ex = new BusinessException("msg");

            Assert.IsAssignableFrom<HrmsException>(ex);
            Assert.IsAssignableFrom<Exception>(ex);
        }

        // ─── NotFoundException ────────────────────────────────────────────────────

        [Fact]
        public void NotFoundException_WithResourceNameAndId_ProducesExpectedMessage()
        {
            var ex = new NotFoundException("Employee", 42);

            Assert.Equal("NOT_FOUND", ex.ErrorCode);
            Assert.Contains("Employee", ex.Message);
            Assert.Contains("42", ex.Message);
        }

        [Fact]
        public void NotFoundException_WithCustomMessage_StoresMessage()
        {
            var ex = new NotFoundException("Custom not-found message.");

            Assert.Equal("NOT_FOUND", ex.ErrorCode);
            Assert.Equal("Custom not-found message.", ex.Message);
        }

        [Fact]
        public void NotFoundException_IsHrmsException()
        {
            var ex = new NotFoundException("Resource", 1);

            Assert.IsAssignableFrom<HrmsException>(ex);
        }

        // ─── Polymorphism ─────────────────────────────────────────────────────────

        [Fact]
        public void HrmsExceptions_CanBeCaughtByBaseType()
        {
            static void ThrowBusiness() => throw new BusinessException("biz");
            static void ThrowNotFound() => throw new NotFoundException("Resource", 99);

            Assert.Throws<BusinessException>(ThrowBusiness);
            Assert.Throws<NotFoundException>(ThrowNotFound);

            // Both are catchable as HrmsException (use ThrowsAny for assignability check)
            Assert.ThrowsAny<HrmsException>(ThrowBusiness);
            Assert.ThrowsAny<HrmsException>(ThrowNotFound);
        }
    }
}
