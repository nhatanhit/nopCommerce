using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace Nop.Services.RabbitMQ.Interfaces;
public interface IChannelDeclarationService
{
    public Task SetupRabbitMQConnection(CancellationToken cancellationToken);
    public Task CreateQueueAsync(string queueName);

    public Task StopRabbitMQConnection(CancellationToken cancellationToken);

    public IChannel GetChannel();
}
