using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Services.RabbitMQ.MessageModel;
public class Store
{
    public string Name { get; set; }

    public string DockerImageName { get; set; }

    public int? HttpPort { get; set; }

    public int? HttpsPort { get; set; }

}
