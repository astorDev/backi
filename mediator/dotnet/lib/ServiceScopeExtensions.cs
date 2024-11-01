using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Backi;

public static class ServiceScopeExtensions
{
    public static async Task SendMediatorRequest<TRequest>(this IServiceScopeFactory serviceScopeFactory, ILogger? logger = null, CancellationToken? cancellationToken = null)
        where TRequest : IRequest
    {
        logger?.LogDebug("Sending {requestType} to mediator in a new scope", typeof(TRequest));
        using var scope = serviceScopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var request = (TRequest)Activator.CreateInstance(typeof(TRequest))!;
        await mediator.Send(request, cancellationToken ?? CancellationToken.None);
        logger?.LogInformation("{requestType} processed by mediator", typeof(TRequest));
    }
}
