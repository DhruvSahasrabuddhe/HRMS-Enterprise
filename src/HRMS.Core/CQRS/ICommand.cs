namespace HRMS.Core.CQRS
{
    /// <summary>
    /// Marker interface for a command that returns a result.
    /// Commands represent the intent to change state and should have
    /// a single handler that performs the operation.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned after executing the command.</typeparam>
    public interface ICommand<TResult>
    {
    }

    /// <summary>
    /// Marker interface for a command with no return value (fire-and-forget).
    /// </summary>
    public interface ICommand : ICommand<Unit>
    {
    }

    /// <summary>
    /// Represents the absence of a meaningful return value (similar to void but usable generically).
    /// </summary>
    public readonly struct Unit : IEquatable<Unit>
    {
        public static readonly Unit Value = default;
        public bool Equals(Unit other) => true;
        public override bool Equals(object? obj) => obj is Unit;
        public override int GetHashCode() => 0;
        public override string ToString() => "()";
    }
}
