using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Services.RabbitMQ.Consumers;
using Nop.Services.RabbitMQ.Interfaces;

namespace Nop.Services.RabbitMQ.Modules;
public class StoreWorkerModule : IWorkerModule
{
    public string Name => "StoreWorkerModule";

    private readonly IMessageConsumer<StoreConsumer> _storeConsumer;

    public StoreWorkerModule(IMessageConsumer
        <StoreConsumer> storeConsumer)
    {
        _storeConsumer = storeConsumer;
    }

    public Task CloseAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public async Task InitializeAsnyc(CancellationToken cancellationToken)
    {
         _storeConsumer.Initialize();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _storeConsumer.StartListening(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _storeConsumer.StopListening(cancellationToken);
    }
}
