using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace Nop.Services.RabbitMQ.Interfaces;
public interface IMessageConsumer<T>
{
    public string QueueName { get; }
    public string ExchangeName { get; }
    public Task StartListening(CancellationToken cancellationToken = default);

    public Task StopListening(CancellationToken cancellationToken = default);

    public void Initialize();
}
