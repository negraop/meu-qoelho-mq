using Grpc.Core;
using MeuQoelhoMQProto;

namespace src.Domain;

public class Subscriber
{
    public IServerStreamWriter<GetMessageReply> Stream { get; set; }
    public ServerCallContext Context { get; set; }

    public Subscriber(IServerStreamWriter<GetMessageReply> stream, ServerCallContext context)
    {
        Stream = stream;
        Context = context;
    }

    // public async void SendMessage(Message message)
    // {
    //     if (!_cancellationToken.IsCancellationRequested)
    //     {
    //         var content = new MessageContent
    //         {
    //             IdMessage = message.Id.ToString(),
    //             QueueName = message.QueueName,
    //             MessageString = message.ContentString,
    //             MessageBytes = message.ContentBytes
    //         };

    //         await _responseStream.WriteAsync(content);
    //     }
    // }
}
