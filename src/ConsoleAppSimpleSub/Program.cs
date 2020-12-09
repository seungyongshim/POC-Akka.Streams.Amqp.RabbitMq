using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Streams.Amqp.RabbitMq;

namespace ConsoleAppSimpleSub
{
    class Program
    {
        static void Main(string[] args)
        {
            var actorSystem = ActorSystem.Create("consoleSample");

            var subActor = actorSystem.ActorOf(Props.Create<SubActor>(), nameof(SubActor));

            var consoleActor = actorSystem.ActorOf(Props.Create<ConsoleActor>(), nameof(ConsoleActor));

            subActor.Tell(new SubActor.Setup
            (
                "Mirero.MLS.Api.Queue.Hello.Ask",
                new List<(string Host, int Port)>
                {
                    ("localhost", 1234),
                    ("localhost", 5672),
                },
                "mirero",
                "system",
                consoleActor
            ));

            Console.ReadLine();
        }
    }
}
