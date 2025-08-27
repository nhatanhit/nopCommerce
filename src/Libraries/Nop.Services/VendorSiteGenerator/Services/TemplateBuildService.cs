using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nop.Services.RabbitMQ.Interfaces;
using Nop.Services.RabbitMQ.MessageModel;
using Nop.Services.VendorSiteGenerator.Interfaces;
using Nop.Services.VendorSiteGenerator.Utilities;
namespace Nop.Services.VendorSiteGenerator.Services;
public sealed class TemplateBuildService : ITemplateBuildService
{
    private readonly IDockerService _dockerService;
    private readonly ILogger<TemplateBuildService> _log;
    protected readonly IMessageProducer _messageProducer;
    public TemplateBuildService(ILogger<TemplateBuildService> log, IDockerService dockerService, IMessageProducer messageProducer)
    {
        _log = log;
        _dockerService = dockerService;
        _messageProducer = messageProducer;
    }


    public async Task BuildFromTemplateAsync(string projectTemplateRoot,
        string hostName,
        int httpPort,
        int httpsPort,
        string templateName,
        string newProjectName,
        string siteTitle,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(projectTemplateRoot))
            throw new ArgumentException("Project root path required.");

        var rootStoreProjects =  Path.GetFullPath(projectTemplateRoot); 

        // Ensure a clean output path


        var outputDir = Path.Combine(rootStoreProjects, newProjectName);
        if (System.IO.Directory.Exists(outputDir))
        {
            _log.LogInformation("Output folder {OutputDir} exists, deleting...", outputDir);
            System.IO.Directory.Delete(outputDir, recursive: true);
        }

        // 1) dotnet new
        var newArgs = $"new {templateName} -D {hostName} --httpPort {httpPort.ToString()} --httpsPort {httpsPort.ToString()} --storename {Quote(siteTitle)}  -o {newProjectName}";
        _log.LogInformation("Running: dotnet {Args} (wd={WD})", newArgs, rootStoreProjects);
        var newRes = await CommandRunner.RunAsync(
            fileName: "dotnet",
            arguments: newArgs,
            workingDir: rootStoreProjects,
            timeout: TimeSpan.FromMinutes(5));

        if (!newRes.Succeeded)
        {
            _log.LogError("dotnet new failed: {Err}\n{Out}", newRes.StdErr, newRes.StdOut);
            throw new InvalidOperationException($"dotnet new failed: {newRes.StdErr}");
        }

        _log.LogInformation("dotnet new output:\n{Out}", newRes.StdOut);

        // 2) docker build
        // Expect a Dockerfile in outputDir (template should include it). If not, adjust context accordingly.
        string dockerTag = $"{newProjectName.ToLower()}:stores";
        var dockerArgs = $"build -t {Quote(dockerTag)} ./{newProjectName}";
        _log.LogInformation("Running: docker {Args} (wd={WD})", dockerArgs, outputDir);
        var buildRes = await CommandRunner.RunAsync(
            fileName: "docker",
            arguments: dockerArgs,
            workingDir: outputDir,
            timeout: TimeSpan.FromMinutes(15),
            ct: ct);

        _log.LogInformation("docker build output:\n{Out}", buildRes.StdErr);

        //3 check whether the docker image is created
        var dockerImage = await _dockerService.GetDockerImageByTagAsync(dockerTag);
        if (dockerImage != null)
        {
            await _messageProducer.PublishMessageAsync("notification", new StoreNotification()
            {
                TagName = dockerTag
            });
            _log.LogInformation("docker image {Out} build success", dockerTag); 
        }
        else
        {
            _log.LogInformation("docker build failed");
        }
    }

    private static string Quote(string s) =>
       OperatingSystem.IsWindows() ? $"\"{s}\"" : $"\"{s.Replace("\"", "\\\"")}\"";
}
 