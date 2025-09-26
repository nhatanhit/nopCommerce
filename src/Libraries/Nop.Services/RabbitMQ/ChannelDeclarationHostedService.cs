using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Nop.Services.RabbitMQ.Interfaces;
using RabbitMQ.Client;

namespace Nop.Services.RabbitMQ;
public class ChannelDeclarationHostedService : IHostedService
{
    private readonly  IChannelDeclarationService _channelDeclarationService;
    private readonly List<IWorkerModule> _workerModules;
    public ChannelDeclarationHostedService(IChannelDeclarationService channelDeclarationService, IEnumerable<IWorkerModule> workerModules)
    {
        _channelDeclarationService = channelDeclarationService;
        _workerModules = workerModules.ToList();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _channelDeclarationService.SetupRabbitMQConnection(cancellationToken);
        await _channelDeclarationService.CreateExchangeAsync("store.exchange", ExchangeType.Topic);

        await _channelDeclarationService.CreateQueueAsync("store.init");
        await _channelDeclarationService.CreateQueueAsync("store.build.start");
        await _channelDeclarationService.CreateQueueAsync("store.build.end");
        //binding 
        await _channelDeclarationService.BindingQueueWithExchange("store.init", "store.exchange", "store.init");
        await _channelDeclarationService.BindingQueueWithExchange("store.build.start", "store.exchange", "store.build.start");
        await _channelDeclarationService.BindingQueueWithExchange("store.build.end", "store.exchange", "store.build.end");

        await _channelDeclarationService.CreateQueueAsync("notification");
        

        //binding with exchange (topic)
        
        //register consumers
        _workerModules?.ForEach(async (module) =>
        {
            await module.InitializeAsnyc(cancellationToken);
        });
        //start listening
        _workerModules?.ForEach(async (module) =>
        {
            await module.StartAsync(cancellationToken);
        });
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _channelDeclarationService.StopRabbitMQConnection(cancellationToken);
    }
}
