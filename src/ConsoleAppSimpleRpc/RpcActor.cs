using System;
using Akka.Actor;
using Akka.Streams.Amqp.RabbitMq;

namespace ConsoleAppSimpleRpc
{
    public class RpcActor : ReceiveActor, IWithUnboundedStash
    {
        public RpcActor()
        {
            Context.Parent.Tell(new Create());
            Receive<Setup>(Handle);
            ReceiveAny(m => Stash.Stash());
        }

        private void Handle(Setup m)
        {
            Become(Ready);
            Stash.UnstashAll();



        }

        public void Ready() => Receive<object>(Handle);

        public IStash Stash { get; set; }

        public class Create
        {

        }

        public class Setup
        {
            public Setup(IAmqpConnectionSettings connectionSettings, string queueName, IDeclaration queueDeclation)
            {
                ConnectionSettings = connectionSettings;
                QueueName = queueName;
                QueueDeclation = queueDeclation;
            }

            public IAmqpConnectionSettings ConnectionSettings { get; }
            public string QueueName { get; }
            public IDeclaration QueueDeclation { get; }
        }
    }

    
}
