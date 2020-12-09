using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Streams.Amqp.RabbitMq;
using Akka.Streams.Amqp.RabbitMq.Dsl;
using Akka.Streams.Dsl;

namespace AkkaStreamsAmqpHelper
{
    public static class SourcesHelper
    {
        public static Source<CommittableIncomingMessage, Akka.NotUsed> CommittableQueueWithWithBackoff(Action<SimpleQueueOptions> opt)
        {
            return RestartSource.WithBackoff(() => CommittableQueue(opt),
                                           TimeSpan.FromSeconds(3),
                                           TimeSpan.FromSeconds(30),
                                           0.2);
        }

        public static Source<CommittableIncomingMessage, Akka.NotUsed> CommittableQueue(Action<SimpleQueueOptions> opt)
        {
            var option = new SimpleQueueOptions();

            opt.Invoke(option);

            var connectionSettings = AmqpConnectionDetails.Create(option.HostAndPorts.First().Host, option.HostAndPorts.First().Port)
                                                          .WithHostsAndPorts(option.HostAndPorts.First(), option.HostAndPorts.ToArray())
                                                          .WithCredentials(AmqpCredentials.Create(option.UserName, option.Password))
                                                          .WithAutomaticRecoveryEnabled(true)
                                                          .WithNetworkRecoveryInterval(TimeSpan.FromSeconds(1));

            var queueDeclaration = QueueDeclaration.Create(option.QueueName)
                                                   .WithDurable(false)
                                                   .WithAutoDelete(false);

            return AmqpSource.CommittableSource(NamedQueueSourceSettings.Create(connectionSettings, option.QueueName)
                                                                        .WithDeclarations(queueDeclaration), 1);
        }
    }

    public class CommonOptions
    {
        public List<(string Host, int Port)> HostAndPorts { get; set; } = new List<(string, int)>();
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class SimpleQueueOptions : CommonOptions
    {
        public string QueueName { get; set; }
    }
}
