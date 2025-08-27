using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Nop.Core.Configuration;
using Nop.Services.Events;
using Nop.Services.RabbitMQ.Interfaces;
using Nop.Services.RabbitMQ.MessageModel;
using Nop.Services.VendorSiteGenerator.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Nop.Services.RabbitMQ.Consumers;
public class StoreConsumer : IMessageConsumer<StoreConsumer>
{
    public string QueueName => "store";
    private readonly IChannelDeclarationService _channelDeclarationService;
    private readonly  ITemplateBuildService _templateBuildService;
    private AsyncEventingBasicConsumer _consumer;
    private IChannel _channel;
    protected readonly AppSettings _appSettings;
    public StoreConsumer(IChannelDeclarationService channelDeclarationService, ITemplateBuildService templateBuildService, AppSettings appSettings) {
        _channelDeclarationService = channelDeclarationService;
        _templateBuildService = templateBuildService;
        _appSettings = appSettings;
    }
    public void Initialize()
    {
        _channel = _channelDeclarationService.GetChannel();
        _consumer = new AsyncEventingBasicConsumer(_channel);
        _consumer.ReceivedAsync += _consumer_ReceivedAsync;
    }

    private async Task<bool> _consumer_ReceivedAsync(object sender, BasicDeliverEventArgs @event)
    {
        var body = @event.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        var storeObject = JsonConvert.DeserializeObject<Store>(message);

        var storeInitSetting = _appSettings.Get<StoreInitSettings>();
        if (storeObject.HttpPort.HasValue && storeObject.HttpsPort.HasValue)
        {
            await _templateBuildService.BuildFromTemplateAsync(projectTemplateRoot: storeInitSetting.TemplateRootPath,
            templateName: storeInitSetting.TemplateName,
            hostName: storeInitSetting.HostName,
            httpPort: storeObject.HttpPort.Value,
            httpsPort: storeObject.HttpsPort.Value,
            siteTitle: storeObject.Name,
            newProjectName: storeObject.DockerImageName);
        }
        
        return true;
    }

    public async Task StartListening(CancellationToken cancellationToken = default)
    {
        await _channel.BasicConsumeAsync(QueueName, autoAck: true, consumer: _consumer, cancellationToken: cancellationToken);
    }

    
    public Task StopListening(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
