namespace HRMS.Core.CQRS
{
    /// <summary>
    /// Marker interface for a query that returns a result.
    /// Queries represent read operations and must not modify state.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the query.</typeparam>
    public interface IQuery<TResult>
    {
    }
}
