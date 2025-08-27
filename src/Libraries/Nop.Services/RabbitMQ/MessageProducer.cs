using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Nop.Services.RabbitMQ.Interfaces;
using RabbitMQ.Client;

namespace Nop.Services.RabbitMQ;
public class MessageProducer : IMessageProducer
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    public MessageProducer(IServiceScopeFactory serviceScopeFactory) { 
        _serviceScopeFactory = serviceScopeFactory;
    }
    public async Task PublishMessageAsync<T>(string queueName, T messageObject) where T : class
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var channelDeclarationService = scope.ServiceProvider.GetService<IChannelDeclarationService>();
        var channel = channelDeclarationService.GetChannel();
        var serializedString = JsonConvert.SerializeObject(messageObject);

        var body = Encoding.UTF8.GetBytes(serializedString);
        await channel.BasicPublishAsync(exchange: string.Empty, routingKey: queueName, body: body);
        
    }
}
