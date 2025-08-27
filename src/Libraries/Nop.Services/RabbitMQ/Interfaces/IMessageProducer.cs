using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Services.RabbitMQ.Interfaces;
public interface IMessageProducer
{
    public Task PublishMessageAsync<T>(string queueName, T @object) where T : class;
}
