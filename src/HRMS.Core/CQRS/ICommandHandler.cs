using HRMS.Shared.Common;

namespace HRMS.Core.CQRS
{
    /// <summary>
    /// Handles a command and returns a result.
    /// Each command should have exactly one handler.
    /// </summary>
    /// <typeparam name="TCommand">The command type to handle.</typeparam>
    /// <typeparam name="TResult">The type of result returned by the handler.</typeparam>
    public interface ICommandHandler<TCommand, TResult>
        where TCommand : ICommand<TResult>
    {
        Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Handles a command with no return value.
    /// </summary>
    /// <typeparam name="TCommand">The command type to handle.</typeparam>
    public interface ICommandHandler<TCommand> : ICommandHandler<TCommand, Unit>
        where TCommand : ICommand<Unit>
    {
    }
}
