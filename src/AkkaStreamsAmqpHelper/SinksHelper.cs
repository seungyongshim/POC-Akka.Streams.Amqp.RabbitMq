using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.IO;
using Akka.Streams.Amqp.RabbitMq;
using Akka.Streams.Amqp.RabbitMq.Dsl;
using Akka.Streams.Dsl;

namespace AkkaStreamsAmqpHelper
{
    public static class SinksHelper
    {
        public static Sink<ByteString, Akka.NotUsed> SimpleWithBackoff(Action<SimpleQueueOptions> opt)
        {
            return RestartSink.WithBackoff(() => Simple(opt),
                                           TimeSpan.FromSeconds(3),
                                           TimeSpan.FromSeconds(30),
                                           0.2);
        }

        public static Sink<ByteString, Task> Simple(Action<SimpleQueueOptions> opt)
        {
            var option = new SimpleQueueOptions();

            opt.Invoke(option);

            var connectionSettings = AmqpConnectionDetails.Create(option.HostAndPorts.First().Host, option.HostAndPorts.First().Port)
                                                          .WithHostsAndPorts(option.HostAndPorts.First(), option.HostAndPorts.ToArray())
                                                          .WithCredentials(AmqpCredentials.Create(option.UserName, option.Password))
                                                          .WithAutomaticRecoveryEnabled(true)
                                                          .WithNetworkRecoveryInterval(TimeSpan.FromSeconds(1))
                                                          .WithVirtualHost(option.VirtualHost ?? "/");

            var queueDeclaration = QueueDeclaration.Create(option.QueueName)
                                                   .WithDurable(false)
                                                   .WithAutoDelete(false);

            return AmqpSink.CreateSimple(AmqpSinkSettings.Create(connectionSettings)
                                                         .WithRoutingKey(option.QueueName)
                                                         .WithDeclarations(queueDeclaration));
        }
    }
}
