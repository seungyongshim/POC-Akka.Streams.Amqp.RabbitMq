using System.Threading.Tasks;

namespace AkkaStreamsAmqpHelper
{
    public interface ICommitable
    {
        Task Ack();
        Task Nack();
    }
}
