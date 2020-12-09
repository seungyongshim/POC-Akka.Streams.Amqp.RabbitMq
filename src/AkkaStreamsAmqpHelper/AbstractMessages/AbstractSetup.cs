using System;
using System.Collections.Generic;
using System.Text;

namespace AkkaStreamsAmqpHelper.AbstractMessages
{
    public abstract class AbstractSetup
    {
        public string QueueName { get; protected set; }
        public List<(string Host, int Port)> HostAndPorts { get; protected set; }
        public string UserName { get; protected set; }
        public string Password { get; protected set; }
        public string VirtualHost { get; protected set; } = "/rabbitmq";
    }
}
