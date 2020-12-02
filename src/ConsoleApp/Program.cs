using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.IO;
using Akka.Streams;
using Akka.Streams.Amqp.RabbitMq;
using Akka.Streams.Amqp.RabbitMq.Dsl;
using Akka.Streams.Dsl;

namespace ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var actorSystem = ActorSystem.Create("poc");
            using var mat = actorSystem.Materializer();

            var connectionSettings = AmqpConnectionDetails.Create("localhost", 5672)
                                                          .WithCredentials(AmqpCredentials.Create("mirero", "system"))
                                                          .WithAutomaticRecoveryEnabled(true)
                                                          .WithNetworkRecoveryInterval(TimeSpan.FromSeconds(1));

            var queueName = "Amqp_Test_1";

            var queueDeclation = QueueDeclaration.Create(queueName)
                                                 .WithDurable(false)
                                                 .WithAutoDelete(true);

            var amqpSink = AmqpSink.CreateSimple(AmqpSinkSettings.Create(connectionSettings)
                                                                 .WithRoutingKey(queueName)
                                                                 .WithDeclarations(queueDeclation));

            var amqpSource = AmqpSource.CommittableSource(NamedQueueSourceSettings.Create(connectionSettings, queueName)
                                                                                  .WithDeclarations(queueDeclation), 1);

            var input = new[] { "one", "two", "three", "four", "five" };

            await Source.From(input)
                        .Select(ByteString.FromString)
                        .RunWith(amqpSink, mat);

            await actorSystem.Terminate();
        }
    }
}
