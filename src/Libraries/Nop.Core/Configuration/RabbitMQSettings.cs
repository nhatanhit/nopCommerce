using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Core.Configuration;
public partial class RabbitMQSettings : IConfig
{
    public string HostName { get; set; }
    public int Port { get; set; }
    public string UserName { get;set; }

    public string Password { get; set; }    
}
