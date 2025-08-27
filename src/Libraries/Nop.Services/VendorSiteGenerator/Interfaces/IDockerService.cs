using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet.Models;

namespace Nop.Services.VendorSiteGenerator.Interfaces;
public interface IDockerService
{
    public Task<ImageInspectResponse> GetDockerImageByTagAsync(string tagName);
}
