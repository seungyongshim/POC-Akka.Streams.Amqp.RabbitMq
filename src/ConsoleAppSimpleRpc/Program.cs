using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Streams.Amqp.RabbitMq;
using Messages;

namespace ConsoleAppSimpleRpc
{
    class Program
    {
        static void Main(string[] args)
        {
            var actorSystem = ActorSystem.Create("consoleSample");

            var pubActor = actorSystem.ActorOf(Props.Create<RpcActor>(), nameof(RpcActor));

            pubActor.Tell(new RpcActor.Setup
            (
                AmqpConnectionDetails.Create("localhost", 5672)
                                     .WithCredentials(AmqpCredentials.Create("mirero", "system"))
                                     .WithAutomaticRecoveryEnabled(true)
                                     .WithNetworkRecoveryInterval(TimeSpan.FromSeconds(1)),
                "Test1111",

                QueueDeclaration.Create("Test1111")
                                .WithDurable(false)
                                .WithAutoDelete(false)
            ));

            pubActor.Tell(new Hello());

            await Task.Delay(5000);

            await actorSystem.Terminate();
        }
    }
}
