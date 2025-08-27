using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Nop.Core.Configuration;
using Nop.Services.VendorSiteGenerator.Interfaces;

namespace Nop.Services.VendorSiteGenerator.Services;
public class DockerService : IDockerService
{
    protected readonly AppSettings _appSettings;

    public DockerService(AppSettings appSettings) {
        _appSettings = appSettings;
    }
    public async Task<ImageInspectResponse> GetDockerImageByTagAsync(string tagName)
    {
        string dockerEndpoint = GetDockerEndpoint();
        var docker = new DockerClientConfiguration(new Uri(dockerEndpoint)).CreateClient();
        try
        { 
            var inspect = await docker.Images.InspectImageAsync(tagName);
            return inspect;
        }
        catch (DockerImageNotFoundException)
        {
            return null;
        }
    }

    private string GetDockerEndpoint()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "npipe://./pipe/docker_engine";
        return "unix:///var/run/docker.sock";
    }
}
