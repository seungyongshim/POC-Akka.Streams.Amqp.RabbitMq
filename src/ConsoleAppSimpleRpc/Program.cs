using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Streams.Amqp.RabbitMq;
using Messages;

namespace ConsoleAppSimpleRpc
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var actorSystem = ActorSystem.Create("consoleSample");

            var rpcActor = actorSystem.ActorOf(Props.Create<RpcActor>(), nameof(RpcActor));

            rpcActor.Tell(new RpcActor.Setup
            (
                "Mirero.MLS.Api.Queue.Hello.Ask",
                new List<(string, int)>
                {
                    ("localhost", 1234),
                    ("localhost", 5672),
                },
                "mirero",
                "system"
            ));

            var ret = await rpcActor.Ask<World>(new Hello("Hong"));

            await Task.Delay(5000);
            await actorSystem.Terminate();

        }
    }
}
