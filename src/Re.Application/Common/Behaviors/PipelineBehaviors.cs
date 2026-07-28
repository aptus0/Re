using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Re.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline: FluentValidation ile otomatik doğrulama.
/// Handler çalışmadan önce tüm validator'lar çalıştırılır.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any()) return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
            throw new Domain.Exceptions.DomainException(
                "Validation error: " + string.Join("; ", failures.Select(f => f.ErrorMessage)));

        return await next(cancellationToken);
    }
}

/// <summary>
/// MediatR pipeline: Request/Response loglama.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("→ Re Request: {RequestName}", requestName);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await next(cancellationToken);
            sw.Stop();
            _logger.LogInformation("← Re Response: {RequestName} ({ElapsedMs}ms)",
                requestName, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "✕ Re Error: {RequestName} ({ElapsedMs}ms)",
                requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}

