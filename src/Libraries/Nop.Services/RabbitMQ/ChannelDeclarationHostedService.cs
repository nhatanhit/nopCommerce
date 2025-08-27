using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Nop.Services.RabbitMQ.Interfaces;

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
        await _channelDeclarationService.CreateQueueAsync("store");
        await _channelDeclarationService.CreateQueueAsync("notification");
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
