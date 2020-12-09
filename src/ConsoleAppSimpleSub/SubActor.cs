using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;
using Akka.Streams;
using Akka.Streams.Dsl;
using AkkaStreamsAmqpHelper;
using AkkaStreamsAmqpHelper.AbstractMessages;
using Messages;

namespace ConsoleAppSimpleSub
{
    public class SubActor : ReceiveActor, IWithUnboundedStash
    {
        public SubActor()
        {
            Context.Parent.Tell(new Create());
            Receive<Setup>(Handle);
            ReceiveAny(x => Stash.Stash());
        }

        public IStash Stash { get; set; }
        private ILoggingAdapter Logger { get; } = Context.GetLogger();
        public IActorRef ConsoleActor { get; private set; }

        private void Handle(Setup m)
        {
            Become(Ready);
            Stash.UnstashAll();

            ConsoleActor = m.ConsoleActor;

            var source = SourcesHelper.CommittableQueueWithWithBackoff(option =>
            {
                option.HostAndPorts = m.HostAndPorts;
                option.QueueName = m.QueueName;
                option.UserName = m.UserName;
                option.Password = m.Password;
                option.VirtualHost = m.VirtualHost;
            });

            var sinkActor = Sink.ActorRef<(object, ICommitable)>(Self, new CopmleteMessage());

            var graph = GraphDsl.Create(source, (builder, start) =>
            {
                var sink = builder.Add(sinkActor);
                var flow = builder.Add(FlowsHelper.Deserialize());

                builder.From(start)
                       .Via(flow)
                       .To(sink);

                return ClosedShape.Instance;
            });

            Context.Materializer().Materialize(graph);
        }

        private void Ready() => Receive<(object, ICommitable)>(Handle);

        private void Handle((object Message, ICommitable Commit) m)
        {
            switch (m.Message)
            {
                case Hello hello:
                {
                    ConsoleActor.Tell((hello, m.Commit));
                }
                break;
            }
        }

        public class Setup : AbstractSetup
        {
            public Setup(string queueName,
                         List<(string Host, int Port)> hostAndPorts,
                         string userName,
                         string password,
                         IActorRef consoleActor)
            {
                HostAndPorts = hostAndPorts;
                QueueName = queueName;
                UserName = userName;
                Password = password;
                ConsoleActor = consoleActor;
            }
            
            public IActorRef ConsoleActor { get; }
        }

        public record Create;
        public record CopmleteMessage;
    }
}
