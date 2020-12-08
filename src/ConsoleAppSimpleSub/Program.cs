using System;
using Akka.Actor;
using Akka.Streams.Amqp.RabbitMq;

namespace ConsoleAppSimpleSub
{
    class Program
    {
        static void Main(string[] args)
        {
            var actorSystem = ActorSystem.Create("consoleSample");

            var pubActor = actorSystem.ActorOf(Props.Create<SubActor>(), nameof(SubActor));

            pubActor.Tell(new SubActor.Setup
            (
                ConnectionSettings: AmqpConnectionDetails.Create("localhost", 5672)
                                                          .WithCredentials(AmqpCredentials.Create("mirero", "system"))
                                                          .WithAutomaticRecoveryEnabled(true)
                                                          .WithNetworkRecoveryInterval(TimeSpan.FromSeconds(1)),
                QueueName: "Test1111",

                QueueDeclation: QueueDeclaration.Create("Test1111")
                                                 .WithDurable(false)
                                                 .WithAutoDelete(false)
            ));

            Console.ReadLine();
        }
    }
}
