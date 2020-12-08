using System;
using Akka.Actor;
using Akka.Event;
using Akka.IO;
using Akka.Streams;
using Akka.Streams.Amqp.RabbitMq;
using Akka.Streams.Amqp.RabbitMq.Dsl;
using Akka.Streams.Dsl;
using Akka.Util;
using Newtonsoft.Json;

namespace ConsoleAppSimplePub
{
    public class PubActor : ReceiveActor, IWithUnboundedStash
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

        public void Ready()
        {
            Receive<PubMessage>(Handle);
        }

        private void Handle(Setup m)
        {
            Become(Ready);
            Stash.UnstashAll();

            var sinkRabbitMQ = AmqpSink.CreateSimple(AmqpSinkSettings.Create(m.ConnectionSettings)
                                                                     .WithRoutingKey(m.QueueName)
                                                                     .WithDeclarations(m.QueueDeclation));

            var source = Source.ActorRef<object>(5000, OverflowStrategy.DropTail);

            var restartSink = RestartSink.WithBackoff(() => sinkRabbitMQ,
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(30),
            0.2);

            var flowObject = Flow.Create<object>()
                                 .Select(x => JsonConvert.SerializeObject(x, Formatting.Indented, new JsonSerializerSettings
                                 {
                                     TypeNameHandling = TypeNameHandling.All,
                                     TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
                                 }))
                                 .Select(ByteString.FromString)
                                 .Recover(ex =>
                                 {
                                     Logger.Error(ex, "");
                                     return Option<ByteString>.None;
                                 });

            var graph = GraphDsl.Create(source, (builder, start) =>
            {
                var sink = builder.Add(restartSink);
                var flow = builder.Add(flowObject);

                builder.From(start).Via(flow).To(sink);

                return ClosedShape.Instance;
            });

            SourceActor = Context.Materializer().Materialize(graph);
        }

        private void Handle(PubMessage m)
        {
            SourceActor.Tell(m.Message);
        }

        public record Create;
        public record Setup(IAmqpConnectionSettings ConnectionSettings, string QueueName, IDeclaration QueueDeclation);
        public record PubMessage(object Message);
    }
}
