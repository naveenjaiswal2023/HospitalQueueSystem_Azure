using HospitalQueueSystem.Application.Common;
using HospitalQueueSystem.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HospitalQueueSystem.Infrastructure.Events
{
    public class DomainEventPublisher : IDomainEventPublisher
    {
        private readonly IMediator _mediator;
        private readonly ILogger<DomainEventPublisher> _logger;

        public DomainEventPublisher(IMediator mediator, ILogger<DomainEventPublisher> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken) where T : IDomainEvent
        {
            try
            {
                await _mediator.Publish(domainEvent, cancellationToken);
                _logger.LogInformation("Domain event published: {EventType}", domainEvent.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while publishing domain event.");
            }
        }
    }
}
