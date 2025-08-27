using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Configuration;
using Nop.Services.RabbitMQ.Interfaces;
using RabbitMQ.Client;

namespace Nop.Services.RabbitMQ;
public class ChannelDeclarationService : IChannelDeclarationService
{
    protected readonly AppSettings _appSettings;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private IChannel _channel;
    private IConnection _connection;
    public ChannelDeclarationService(AppSettings appSettings, IServiceScopeFactory serviceScopeFactory  )
    {
        _appSettings = appSettings;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task CreateQueueAsync(string queueName)
    {
         await _channel.QueueDeclareAsync(queue: queueName, durable: false, exclusive: false, autoDelete: false,arguments: null);
    }

    public IChannel GetChannel()
    {
        return _channel;
    }

    public async Task SetupRabbitMQConnection(CancellationToken cancellationToken)
    {
        var settings = _appSettings.Get<RabbitMQSettings>();
        var factory = new ConnectionFactory
        {
            HostName = settings.HostName,
            UserName = settings.UserName,
            Password = settings.Password,
            Port = settings.Port
        };
        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
    }

    public async Task StopRabbitMQConnection(CancellationToken cancellationToken)
    {
        await _channel.CloseAsync(cancellationToken: cancellationToken);
        await _connection.CloseAsync(cancellationToken: cancellationToken);
        await _channel.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
