using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Services.VendorSiteGenerator.Interfaces;
public interface ITemplateBuildService
{
    public Task BuildFromTemplateAsync(string projectTemplateRoot, string hostName, int httpPort, int httpsPort, string templateName , string newProjectName,string siteTitle,CancellationToken ct = default);
}
