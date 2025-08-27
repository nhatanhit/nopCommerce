using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Core.Configuration;
public partial class StoreInitSettings : IConfig
{
    public string HostName { get; set; }

    public string TemplateName { get; set; }

    public string TemplateRootPath { get; set; }

    public int HttpFromPort { get; set; }

    public int HttpToPort { get; set; }

    public int HttpsFromPort { get; set; }

    public int HttpsToPort { get; set; }
}
