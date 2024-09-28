using Google.Protobuf;
using Grpc.Core;
using MeuQoelhoMQ.Domain;
using MeuQoelhoMQProto;
using src.Domain;
using src.Services;

namespace MeuQoelhoMQServer;

public class Server
{
    // Quebrei a cabeça por 1 dia inteiro literalmente para tentar resolver um problema
    // que eu estava enfrentando com a lista de filas de mensagens. Basicamente o servidor
    // não estava guardando as filas que eram criadas pelo cliente, então a cada nova 
    // execução de um cliente parecia que uma nova instância do servidor era criada,
    // mesmo sem interromper a execução do servidor (O.o)!
    // Depois de muita pesquisa (o link que me salvou foi esse repositório que contém alguns
    // exemplos gRPC para .NET: https://github.com/grpc/grpc-dotnet/blob/master/examples/README.md)
    // eu consegui resolver esse problema apenas adicionado uma linha no arquivo Startup.cs:
    //
    // builder.Services.AddSingleton<Server>();
    //
    // Dessa forma eu garanto que apenas uma instância do Server será criada, fazendo com
    // que eu consiga guardar o estado do meu servidor.
    
    private List<Queue> _queues = new List<Queue>();

    // No Server eu implementei um sistema de lock para garantir a segurança 
    // do acesso à recursos compartilhados. Já na classe Queue.cs eu implementei
    // essa segurança usando ConcurrentQueue, que é uma lib do .NET indicada para
    // estruturas do tipo Fila (garante a ordem dos elementos) que precisam dessa
    // segurança ao acessar recursos compartilhados.

    public int QueuesCount
    { 
        get
        {
            lock (_queues)
            {
                return _queues.Count;
            }
        }
    }

    public void RestoreQueuesFromBackup()
    {
        BackupService backup = new BackupService();
        _queues = backup.RestoreQueuesFromBackup();
    }

    public QueueReply CreateQueue(CreateQueueRequest request)
    {
        lock (_queues)
        {
            if (_queues.Any(x => x.Name == request.Name))
            {
                return new QueueReply()
                {
                    Success = false,
                    Message = $"Fila '{request.Name}' já existente"
                };
            }

            Queue queue = new Queue(request.Name, request.Type);
            _queues.Add(queue);

            return new QueueReply()
            {
                Success = true,
                Message = $"Fila '{queue.Name}' criada com sucesso!",
                IdQueue = queue.Id.ToString()
            };
        }
        
    }

    public QueueReply DeleteQueue(string name)
    {
        lock (_queues)
        {
            var queue = _queues.Where(x => x.Name == name).FirstOrDefault();

            if (queue == null)
            {
                return new QueueReply()
                {
                    Success = false,
                    Message = $"Fila '{name}' não encontrada"
                };
            }

            if (queue.MessagesCount > 0)
            {
                return new QueueReply()
                {
                    Success = false,
                    Message = $"Fila '{name}' possui mensagens pendentes"
                };
            }

            int index = _queues.IndexOf(queue);

            _queues.RemoveAt(index);

            return new QueueReply()
            {
                Success = true,
                Message = $"Fila '{queue.Name}' excluída com sucesso!",
                IdQueue = queue.Id.ToString()
            };
        }
    }

    public QueueListReply GetQueues()
    {
        List<QueueSummary> lista = new List<QueueSummary>();

        lock (_queues)
        {
            foreach (var queue in _queues)
            {
                lista.Add(new QueueSummary()
                {
                    IdQueue = queue.Id.ToString(),
                    Name = queue.Name,
                    Type = queue.Type,
                    MessagesCount = queue.MessagesCount
                });
            }
        }
        
        var response = new QueueListReply();
        response.Success = true;
        response.Queues.AddRange(lista);

        return response;
    }

    public (MessageReply messageReply, Queue? queue) PublishMessageBase(PublishMessageRequest request)
    {
        // Valida se a mensagem possui algum conteúdo
        if (request.MessageBytes.IsEmpty && string.IsNullOrWhiteSpace(request.MessageString))
            return (new MessageReply { Success = false, Message = $"A mensagem está em branco!" }, null);

        Message message = new Message(request.MessageString, request.MessageBytes);
        Queue? queue;

        lock (_queues)
        {
            queue = _queues.FirstOrDefault(x => x.Name == request.QueueName);
        }

        // Outra validação, caso não exista a fila que a mensagem será adicionada
        if (queue == null)
            return (new MessageReply { Success = false, Message = $"A fila '{request.QueueName}' não existe!" }, null);

        // Publica a mensagem
        var index = queue.PublishMessage(message);

        // Salva no disco
        BackupService.SaveMessageInDisk(request, message.Id, index, queue.Type);

        var reply = new MessageReply 
        {
            Success = true, 
            Message = $"Mensagem publicada na fila '{queue.Name}' com sucesso!",
            IdMessage = message.Id.ToString()    
        };

        return (reply, queue);
    }

    public MessageReply PublishMessage(PublishMessageRequest request)
    {
        var reply = PublishMessageBase(request);
        
        if (reply.queue != null)
            // Notifica os assinantes
            reply.queue.NotifySubscribers();

        return reply.messageReply;
    }

    public MessagesReply PublishMessages(PublishMessagesRequest request)
    {
        MessagesReply response = new MessagesReply();

        for (var i = 0; i < request.Messages.Count; i++)
        {
            var message = request.Messages[i];
            var reply = PublishMessageBase(message);
            response.MessagesResponse.Add(reply.messageReply);

            if (i == 0 && reply.queue != null)
            {
                // Notifica os assinantes
                reply.queue.NotifySubscribers();
            }
        }

        response.Success = true;
        return response;
    }

    public GetMessageReply GetMessage(GetMessageRequest request)
    {
        Queue? queue;

        lock (_queues)
        {
            queue = _queues.FirstOrDefault(x => x.Name == request.QueueName);
        }

        if (queue == null)
            return new GetMessageReply { Success = false, ResponseMessage = $"A fila '{request.QueueName}' não existe!", Message = new MessageContent() };

        Message? message = queue.GetMessage();

        if (message == null)
            return new GetMessageReply { Success = false, ResponseMessage = $"A fila {request.QueueName} não possui mensagens", Message = new MessageContent() };

        BackupService.RemoveMessageFromDisk(request.QueueName, message.Id);

        GetMessageReply response = new GetMessageReply()
        { 
            Success = true, 
            ResponseMessage = $"Mensagem resgatada na fila '{request.QueueName}' com sucesso!", 
            Message = new MessageContent() 
            {
                QueueName = request.QueueName, 
                IdMessage = message.Id.ToString(),
                MessageString = message.ContentString,
                MessageBytes = message.ContentBytes
            }
        };

        return response;
    }

    public async Task Subscribe(SubscribeRequest request, IServerStreamWriter<GetMessageReply> responseStream, ServerCallContext context)
    {
        Queue? queue;

        lock (_queues)
        {
            queue = _queues.FirstOrDefault(x => x.Name == request.QueueName);
        }

        if (queue == null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Fila '{request.QueueName}' não existe."));

        var subscriber = new Subscriber(responseStream, context);

        queue.Subscribe(subscriber);

        try
        {
            while (!context.CancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000); // Loop infinito, até receber um CancellationToken
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro no Subscribe: {ex.Message}");
        }
        finally
        {
            queue.Unsubscribe(subscriber);
        }
    }
}
