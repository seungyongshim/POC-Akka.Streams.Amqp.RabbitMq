using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.IO;
using Akka.Streams.Amqp.RabbitMq;
using Akka.Streams.Amqp.RabbitMq.Dsl;
using Akka.Streams.Dsl;
using Newtonsoft.Json;

namespace AkkaStreamsAmqpHelper
{
    public static class FlowsHelper
    {
        public static Flow<object, ByteString, Akka.NotUsed> Serialize() =>
            from msg in Flow.Create<object>()
            let json = JsonConvert.SerializeObject(msg, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
            })
            select ByteString.FromString(json);

        public static Flow<CommittableIncomingMessage, (object o, ICommitable c), Akka.NotUsed> Deserialize() =>
            from commitable in Flow.Create<CommittableIncomingMessage>()
            let m = commitable.Message.Bytes.ToString(Encoding.UTF8)
            let o = JsonConvert.DeserializeObject(m, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
            })
            let r = commitable.Message.Properties.ReplyTo
            let c = new Commit(commitable) as ICommitable
            select (o, c);

        public static Flow<OutgoingMessage, CommittableIncomingMessage, Task<string>> SimpleRpc(Action<SimpleQueueOptions> opt) 
        {
            var option = new SimpleQueueOptions();
            opt?.Invoke(option);

            var connectionSettings = AmqpConnectionDetails.Create(option.HostAndPorts.First().Host, option.HostAndPorts.First().Port)
                                                          .WithHostsAndPorts(option.HostAndPorts.First(), option.HostAndPorts.ToArray())
                                                          .WithCredentials(AmqpCredentials.Create(option.UserName, option.Password))
                                                          .WithAutomaticRecoveryEnabled(true)
                                                          .WithNetworkRecoveryInterval(TimeSpan.FromSeconds(1))
                                                          .WithVirtualHost(option.VirtualHost ?? "/");

            var queueDeclaration = QueueDeclaration.Create(option.QueueName)
                                                   .WithDurable(false)
                                                   .WithAutoDelete(false);


            return AmqpRpcFlow.CommittableFlow(AmqpSinkSettings.Create(connectionSettings)
                                                               .WithRoutingKey(option.QueueName)
                                                               .WithDeclarations(queueDeclaration), 1);
        }
    }
}
