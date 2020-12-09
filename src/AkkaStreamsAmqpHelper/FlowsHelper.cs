using System.Text;
using Akka.IO;
using Akka.Streams.Amqp.RabbitMq.Dsl;
using Akka.Streams.Dsl;
using Newtonsoft.Json;

namespace AkkaStreamsAmqpHelper
{
    public static class FlowsHelper
    {
        public static Flow<object, ByteString, Akka.NotUsed> Serialize() =>
            from msg in Flow.Create<object>()
            let json = JsonConvert.SerializeObject(msg, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
            })
            select ByteString.FromString(json);

        public static Flow<CommittableIncomingMessage, (object o, ICommitable c), Akka.NotUsed> Deserialize() =>
            from commitable in Flow.Create<CommittableIncomingMessage>()
            let m = commitable.Message.Bytes.ToString(Encoding.UTF8)
            let o = JsonConvert.DeserializeObject(m, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
            })
            let c = new Commit(commitable) as ICommitable
            select (o, c);
    }
}
