using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Services.RabbitMQ.MessageModel;
public abstract class MessageNotification
{
    public abstract string Type { get; }
}
