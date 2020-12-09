using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Streams.Amqp.RabbitMq;
using Messages;

namespace ConsoleAppSimplePub
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var config = ConfigurationFactory.ParseString(@"
akka {  
    stdout-loglevel = DEBUG
    loglevel = DEBUG
    log-config-on-start = on        
    actor {                
        debug {  
              receive = on 
              autoreceive = on
              lifecycle = on
              event-stream = on
              unhandled = on
        }
}");

            var actorSystem = ActorSystem.Create("consoleSample", config);

            var pubActor = actorSystem.ActorOf(Props.Create<PubActor>(), nameof(PubActor));

            pubActor.Tell(new PubActor.Setup
            (
                "Mirero.MLS.Api.Queue.Hello",
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
