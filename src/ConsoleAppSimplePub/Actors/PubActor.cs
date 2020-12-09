using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;
using Akka.IO;
using Akka.Streams;
using Akka.Streams.Amqp.RabbitMq;
using Akka.Streams.Amqp.RabbitMq.Dsl;
using Akka.Streams.Dsl;
using Akka.Util;
using AkkaStreamsAmqpHelper;
using Newtonsoft.Json;

namespace ConsoleAppSimplePub
{
    public partial class PubActor : ReceiveActor, IWithUnboundedStash
    {
        public PubActor()
        {
            Context.Parent.Tell(new Create());
            Receive<Setup>(Handle);
            ReceiveAny(m => Stash.Stash());
        }

        public IStash Stash { get; set; }
        public IActorRef SourceActor { get; private set; }
        private ILoggingAdapter Logger { get; } = Context.GetLogger();

        public void Ready() => Receive<object>(Handle);

        private void Handle(Setup m)
        {
            Become(Ready);
            Stash.UnstashAll();

            var source = Source.ActorRef<object>(5000, OverflowStrategy.DropTail);

            var graph = GraphDsl.Create(source, (builder, start) =>
            {
                var sink = builder.Add(SinksHelper.SimpleWithBackoff(option =>
                {
                    option.HostAndPorts = m.HostAndPorts;
                    option.UserName = m.UserName;
                    option.Password = m.Password;
                    option.QueueName = m.QueueName;
                }));

                var flow = builder.Add(FlowsHelper.Serialize()
                                                  .Recover(ex =>
                                                  {
                                                      Logger.Error(ex, "");
                                                      return Option<ByteString>.None;
                                                  }));

                builder.From(start)
                       .Via(flow)
                       .To(sink);

                return ClosedShape.Instance;
            });

            SourceActor = Context.Materializer().Materialize(graph);
        }

        private void Handle(object m) => SourceActor.Tell(m);

        public class Create { }

        public class Setup
        {
            public Setup(string queueName, List<(string Host, int Port)> hostAndPorts, string userName, string password)
            {
                QueueName = queueName;
                HostAndPorts = hostAndPorts;
                UserName = userName;
                Password = password;
            }

            public string QueueName { get; }
            public List<(string Host, int Port)> HostAndPorts { get; internal set; }
            public string UserName { get; internal set; }
            public string Password { get; internal set; }
        }
    }
}
