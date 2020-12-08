using System;
using System.Text;
using Akka.Actor;
using Akka.Event;
using Akka.IO;
using Akka.Streams;
using Akka.Streams.Amqp.RabbitMq;
using Akka.Streams.Amqp.RabbitMq.Dsl;
using Akka.Streams.Dsl;
using Akka.Util;
using Newtonsoft.Json;

namespace ConsoleAppSimpleSub
{
    public class SubActor : ReceiveActor, IWithUnboundedStash
    {
        ILoggingAdapter Logger { get; } = Context.GetLogger();

        public SubActor()
        {
            Context.Parent.Tell(new Create());
            Receive<Setup>(Handle);
            ReceiveAny(x => Stash.Stash());
        }

        private void Handle(Setup m)
        {
            Become(Ready);
            Stash.UnstashAll();

            var sourceRabbitMQ = AmqpSource.CommittableSource(NamedQueueSourceSettings.Create(m.ConnectionSettings, m.QueueName)
                                                                                     .WithDeclarations(m.QueueDeclation), 1);

            var restartSource = RestartSource.WithBackoff(() => sourceRabbitMQ,
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(30),
            0.2);

            var flowObject = Flow.Create<CommittableIncomingMessage>()
                                 .Select(m => m.Message.Bytes.ToString(Encoding.UTF8))
                                 .Select(m => JsonConvert.DeserializeObject(m, new JsonSerializerSettings
                                 {
                                     TypeNameHandling = TypeNameHandling.All,
                                 }));

            var sinkActor = Sink.ActorRef<object>(Self, new CopmleteMessage());

            var graph = GraphDsl.Create(restartSource, (builder, start) =>
            {
                var sink = builder.Add(sinkActor);
                var flow = builder.Add(flowObject);

                builder.From(start)
                       .Via(flow)
                       .To(sink);

                return ClosedShape.Instance;
            });

            Context.Materializer().Materialize(graph);
        }

        private void Ready()
        {
            Receive<object>(m => Console.WriteLine(m.GetType().Name));
        }

        public IStash Stash { get; set; }

        public record Setup(IAmqpConnectionSettings ConnectionSettings, string QueueName, IDeclaration QueueDeclation);
        public record Create;
        public record CopmleteMessage;
    }
}
