using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Services.RabbitMQ.Interfaces;
public interface IWorkerModule : IDisposable
{
    string Name { get; }
    public Task InitializeAsnyc(CancellationToken cancellationToken);
    public Task StartAsync(CancellationToken cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken);

    public Task CloseAsync(CancellationToken cancellationToken);


}
