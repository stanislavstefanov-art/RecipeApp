using MediatR;
using Recipes.Domain.Events;

namespace Recipes.Infrastructure.Events;

public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent)
    : INotification
    where TDomainEvent : IDomainEvent;
