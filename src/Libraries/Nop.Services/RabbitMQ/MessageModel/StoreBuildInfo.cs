using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Services.RabbitMQ.MessageModel;
public class StoreBuildInfo
{
    public string RootStoreProject { get; set; }
    public string DockerTag { get; set; }

    public string ProjectName { get; set; }

    public string DockerFileWorkingDirectory { get; set; }

}
