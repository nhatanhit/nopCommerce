using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Services.RabbitMQ.MessageModel;
public class StoreNotification : MessageNotification
{
    public string TagName { get; set; }

    public override string Type => "Store";
}
