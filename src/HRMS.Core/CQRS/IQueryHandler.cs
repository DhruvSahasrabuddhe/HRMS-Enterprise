using HRMS.Shared.Common;

namespace HRMS.Core.CQRS
{
    /// <summary>
    /// Handles a query and returns its result.
    /// Each query should have exactly one handler.
    /// </summary>
    /// <typeparam name="TQuery">The query type to handle.</typeparam>
    /// <typeparam name="TResult">The type of result returned by the handler.</typeparam>
    public interface IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
    }
}
