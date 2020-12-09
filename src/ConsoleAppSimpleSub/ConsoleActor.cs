using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using AkkaStreamsAmqpHelper;
using Messages;

namespace ConsoleAppSimpleSub
{
    public class ConsoleActor : ReceiveActor
    {
        public ConsoleActor()
        {
            ReceiveAsync<(Hello Message, ICommitable Commit)>(async x =>
            {
                Console.WriteLine(x.Message.ToString());

                await Task.Delay(TimeSpan.FromSeconds(30));
                await x.Commit.Ack();
            });
        }
    }
}
