namespace HRMS.Core.ValueObjects
{
    /// <summary>
    /// Abstract base for all value objects. Value objects are equal when all
    /// their components are equal (structural equality), unlike entities which
    /// use identity equality.
    /// </summary>
    public abstract class BaseValueObject : IEquatable<BaseValueObject>
    {
        protected abstract IEnumerable<object?> GetEqualityComponents();

        public bool Equals(BaseValueObject? other)
        {
            if (other is null || other.GetType() != GetType())
                return false;

            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        public override bool Equals(object? obj) => Equals(obj as BaseValueObject);

        public override int GetHashCode() =>
            GetEqualityComponents()
                .Aggregate(17, (hash, component) => hash * 31 + (component?.GetHashCode() ?? 0));

        public static bool operator ==(BaseValueObject? left, BaseValueObject? right) =>
            left?.Equals(right) ?? right is null;

        public static bool operator !=(BaseValueObject? left, BaseValueObject? right) =>
            !(left == right);
    }
}
