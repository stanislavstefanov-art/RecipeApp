using MediatR;
using Recipes.Application.Abstractions;
using Recipes.Domain.Events;

namespace Recipes.Infrastructure.Events;

public sealed class MediatRDomainEventDispatcher(IPublisher publisher) : IDomainEventDispatcher
{
    public async Task DispatchAsync(
        IReadOnlyList<IDomainEvent> events,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            var notificationType = typeof(DomainEventNotification<>)
                .MakeGenericType(domainEvent.GetType());

            var notification = (INotification)Activator.CreateInstance(
                notificationType, domainEvent)!;

            await publisher.Publish(notification, cancellationToken);
        }
    }
}
