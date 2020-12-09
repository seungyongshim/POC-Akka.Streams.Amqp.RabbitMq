using System;
using System.Threading.Tasks;
using Akka.Streams.Amqp.RabbitMq.Dsl;

namespace AkkaStreamsAmqpHelper
{
    public class Commit : ICommitable
    {
        public Commit(CommittableIncomingMessage m)
        {
            AckCommand = m.Ack;
            NackCommand = m.Nack;
        }

        private Func<bool, Task> AckCommand { get; }
        private Func<bool, bool, Task> NackCommand { get; }

        public Task Ack() => AckCommand?.Invoke(false);

        public Task Nack() => NackCommand?.Invoke(false, true);
    }
}
