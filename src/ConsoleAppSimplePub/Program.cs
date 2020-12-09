using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Streams.Amqp.RabbitMq;
using Messages;

namespace ConsoleAppSimplePub
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var actorSystem = ActorSystem.Create("consoleSample");

            var pubActor = actorSystem.ActorOf(Props.Create<PubActor>(), nameof(PubActor));

            pubActor.Tell(new PubActor.Setup
            (
                "Mirero.MLS.Api",
                new List<(string Host, int Port)>
                {
                    ("localhost", 1234),
                    ("localhost", 5672),
                },
                "mirero",
                "system"
            ));

            pubActor.Tell(new Hello("Hong"));
            pubActor.Tell(new Hello("Shim"));
            pubActor.Tell(new Hello("Joe"));

            await Task.Delay(5000);

            await actorSystem.Terminate();
        }
    }
}
