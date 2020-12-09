using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Akka.IO;
using Akka.Streams;
using Akka.Streams.Amqp.RabbitMq;
using Akka.Streams.Dsl;
using Akka.Util;
using AkkaStreamsAmqpHelper;
using AkkaStreamsAmqpHelper.AbstractMessages;
using Messages;

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

        public IStash Stash { get; set; }
        public IActorRef SourceActor { get; private set; }
        public IActorRef Asker { get; private set; }
        private ILoggingAdapter Logger { get; } = Context.GetLogger();

        public void Ready()
        {
            Receive<object>(Handle);
        }

        public void ResponseWait()
        {
            ReceiveAsync<(object, ICommitable)>(Handle);
            ReceiveAny(x => Stash.Stash());
        }

        private void Handle(Setup m)
        {
            Become(Ready);
            Stash.UnstashAll();

            var source = Source.ActorRef<object>(5000, OverflowStrategy.DropTail);

            var sinkActor = Sink.ActorRef<(object, ICommitable)>(Self, new CompleteMessage());

            var graph = GraphDsl.Create(source, (builder, start) =>
            {
                var serializeFlow = builder.Add(FlowsHelper.Serialize()
                                                           .Recover(ex =>
                                                           {
                                                               Logger.Error(ex, "");
                                                               return Option<ByteString>.None;
                                                           })
                                                           .Select(x => new OutgoingMessage(x, true, true)));

                var rpcFlow = builder.Add(FlowsHelper.SimpleRpc(option =>
                {
                    option.HostAndPorts = m.HostAndPorts;
                    option.UserName = m.UserName;
                    option.Password = m.Password;
                    option.QueueName = m.QueueName;
                    option.VirtualHost = m.VirtualHost;
                }));

                builder.From(start)
                       .Via(serializeFlow)
                       .Via(rpcFlow)
                       .Via(FlowsHelper.Deserialize())
                       .To(sinkActor);

                return ClosedShape.Instance;
            });

            SourceActor = Context.Materializer().Materialize(graph);
        }

        private void Handle(object m)
        {
            Asker = Sender;
            SourceActor.Tell(m);
            Become(ResponseWait);
        }

        private async Task Handle((object Message, ICommitable Commit) m)
        {
            switch (m.Message)
            {
                case World msg:
                    Asker.Tell(msg);
                    await m.Commit.Ack();
                break;
                default:
                    Logger.Warning(m.Message.ToString());
                    await m.Commit.Ack();
                break;
            }
        }

        public class Create
        {
        }

        public class Setup : AbstractSetup
        {
            public Setup(string queueName, List<(string Host, int Port)> hostAndPorts, string userName, string password)
            {
                QueueName = queueName;
                HostAndPorts = hostAndPorts;
                UserName = userName;
                Password = password;
            }
        }

        private class CompleteMessage { }
    }
}
