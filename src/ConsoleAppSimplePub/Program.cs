using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Streams.Amqp.RabbitMq;

namespace ConsoleAppSimplePub
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var actorSystem = ActorSystem.Create("consoleSample");

            var pubActor = actorSystem.ActorOf(Props.Create<PubActor>(), nameof(PubActor));

            pubActor.Tell(new PubActor.Setup
            (
                ConnectionSettings : AmqpConnectionDetails.Create("localhost", 5672)
                                                          .WithCredentials(AmqpCredentials.Create("mirero", "system"))
                                                          .WithAutomaticRecoveryEnabled(true)
                                                          .WithNetworkRecoveryInterval(TimeSpan.FromSeconds(1)),
                QueueName : "Test1111",

                QueueDeclation : QueueDeclaration.Create("Test1111")
                                                 .WithDurable(false)
                                                 .WithAutoDelete(false)
            ));

            pubActor.Tell(new PubActor.PubMessage("Hello"));
            pubActor.Tell(new PubActor.PubMessage("World"));
            pubActor.Tell(new PubActor.PubMessage(new List<int> { 1, 2, 3 }));
            pubActor.Tell(new PubActor.PubMessage(new List<Hello> { new Hello() }));

            Console.ReadLine();
        }
    }
}
