using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using MeuQoelhoMQProto;
using static MeuQoelhoMQProto.BrokerService;

namespace MeuQoelhoMQClient;

internal class Client
{
    // Este cliente comtemplará várias chamadas de métodos ao Message Broker para
    // testar as funcionalidades.

    private static async Task Main(string[] args)
    {
        using var channel = GrpcChannel.ForAddress("http://localhost:5001");
        var client = new BrokerServiceClient(channel);

        // Aqui estou testando a funcionalidade de Deadline (a.k.a Timeout)
        // Fonte: https://learn.microsoft.com/en-us/aspnet/core/grpc/deadlines-cancellation?view=aspnetcore-8.0
        // Nesse primeiro caso, adicionei bastante tempo para concluir a requisição
        try
        {   
            // Cria uma fila
            await CreateQueue(client, "Queue1", QueueType.Simple, 5000);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
        {
            Console.WriteLine($"\n*** Response ***\nTIMEOUT!");
        }
        
        // Nesse outro exemplo vou adicionar um Deadline curto para disparar a exceção.
        try
        {
            await CreateQueue(client, "Queue1", QueueType.Simple, 1);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
        {
            Console.WriteLine($"\n*** Response ***\nTIMEOUT!");
        }

        // Tento criar uma fila já existente, retorna uma mensagem dizendo que a fila já existe
        await CreateQueue(client, "Queue1", QueueType.Simple, 5000);

        // Cria uma segunda fila do tipo multiple
        await CreateQueue(client, "Queue2", QueueType.Multiple, 5000);

        // Cria uma terceira fila
        await CreateQueue(client, "Queue3", QueueType.Simple, 5000);

        // Exclui a terceira fila
        await DeleteQueue(client, "Queue3", 5000);

        // Tento excluir uma fila que não existe, retorna uma mensagem dizendo que a fila não existe
        await DeleteQueue(client, "Queue3", 5000);

        // Publica uma mensagem unary RPC
        await PublishMessageString(client, "Queue1", "Hello World!", 5000);

        // Tenta publicar uma mensagem em uma fila inexistente, retorna uma mensagem dizendo que a fila não existe
        await PublishMessageString(client, "Queue3", "Hello World!", 5000);

        // Publica uma lista de mensagens (unary RPC)
        await PublishMessages(client, 5000);

        // Resgata uma mensagem da fila
        await GetMessage(client, "Queue2", 5000);

        // Retorna a lista de filas, incluindo o tipo e a quantidade de mensagens restantes
        await GetQueues(client, 5000);

        

        // Lista de assinaturas de filas separadas por Tasks
        // Obs: Caso exista mensagens na fila e nao tenha nenhum assinante, quando um novo
        // assinante surgir, ele não irá receber as mensagens pendentes. Caso queira consumir
        // as mensagens antes de sua assinatura, deverá resgatá-las via unary RPC.
        var subscriptionTasks = new List<Task>();
        Task task = SubscribeQueue(client, "Queue1", 10000000);
        Task task2 = SubscribeQueue(client, "Queue2", 10000000);
        subscriptionTasks.Add(task);
        subscriptionTasks.Add(task2);

        // Aguarda as mensagens de suas assinaturas ou até atingir o deadline
        await Task.WhenAll(subscriptionTasks);
    }




    private static async Task CreateQueue(BrokerServiceClient client, string name, QueueType type, int deadline)
    {
        var reply = await client.CreateQueueAsync(new CreateQueueRequest()
        {
            Name = name,
            Type = type
        }, deadline: DateTime.UtcNow.AddMilliseconds(deadline));

        Console.WriteLine($"\n*** Response ***\nSuccess: {reply.Success}\nMessage: {reply.Message}\nIdQueue: {reply.IdQueue}");
    }

    private static async Task DeleteQueue(BrokerServiceClient client, string name, int deadline)
    {
        var reply = await client.DeleteQueueAsync(new DeleteQueueRequest()
        {
            Name = name
        }, deadline: DateTime.UtcNow.AddMilliseconds(deadline));

        Console.WriteLine($"\n*** Response ***\nSuccess: {reply.Success}\nMessage: {reply.Message}\nIdQueue: {reply.IdQueue}");
    }

    private static async Task GetQueues(BrokerServiceClient client, int deadline)
    {
        var list = await client.GetQueuesAsync(new Empty(), deadline: DateTime.UtcNow.AddMilliseconds(deadline));

        Console.WriteLine("\nFilas já criadas:");
        foreach (var queue in list.Queues)
        {
            Console.WriteLine($"Name: {queue.Name}, Type: {queue.Type}, MessagesCount: {queue.MessagesCount}");
        }
    }

    private static Task SubscribeQueue(BrokerServiceClient client, string name, int deadline)
    {
        var reply = client.SubscribeQueue(new SubscribeRequest()
        {
            QueueName = name
        }, deadline: DateTime.UtcNow.AddMilliseconds(deadline));

        var task = Task.Run(async () =>
        {
            await foreach (var message in reply.ResponseStream.ReadAllAsync())
            {
                Console.WriteLine($"\nMensagem recebida na fila {name}:\nSuccess: {message.Success}\nResponseMessage: {message.ResponseMessage}\nMessageId: {message.Message.IdMessage}\nMessageContent: {message.Message.MessageString}");
            }
        });

        return task;
    }

    private static async Task PublishMessageString(BrokerServiceClient client, string name, string messageString, int deadline)
    {
        var reply = await client.PublishMessageAsync(new PublishMessageRequest 
        {
            QueueName = name, 
            MessageString = messageString
        }, deadline: DateTime.UtcNow.AddMilliseconds(deadline));

        Console.WriteLine($"\n*** Response ***\nSuccess: {reply.Success}\nMessage: {reply.Message}\nIdMessage: {reply.IdMessage}");
    }

    private static async Task PublishMessageByte(BrokerServiceClient client, string name, ByteString messageByte, int deadline)
    {
        var reply = await client.PublishMessageAsync(new PublishMessageRequest 
        {
            QueueName = name, 
            MessageBytes = messageByte
        }, deadline: DateTime.UtcNow.AddMilliseconds(deadline));

        Console.WriteLine($"\n*** Response ***\nSuccess: {reply.Success}\nMessage: {reply.Message}\nIdMessage: {reply.IdMessage}");
    }

    private static async Task PublishMessages(BrokerServiceClient client, int deadline)
    {
        var messages = new List<PublishMessageRequest>()
        {
            new PublishMessageRequest() { QueueName = "Queue2", MessageString = "Test1" },
            new PublishMessageRequest() { QueueName = "Queue2", MessageString = "Test2" },
            new PublishMessageRequest() { QueueName = "Queue2", MessageString = "Test3" }
        };
        var messagesRequest = new PublishMessagesRequest();
        messagesRequest.Messages.AddRange(messages);

        var reply = await client.PublishMessagesAsync(messagesRequest, deadline: DateTime.UtcNow.AddMilliseconds(deadline));

        Console.WriteLine($"\n*** Response ***\nSuccess: {reply.Success}\nMensagens publicadas com sucesso!");
    }

    private static async Task GetMessage(BrokerServiceClient client, string name, int deadline)
    {
        var reply = await client.GetMessageAsync(new GetMessageRequest()
        {
            QueueName = name
        }, deadline: DateTime.UtcNow.AddMilliseconds(deadline));

        Console.WriteLine($"\n*** Response ***\nSuccess: {reply.Success}\nResponseMessage: {reply.ResponseMessage}\nMessageId: {reply.Message.IdMessage}\nMessageContent: {reply.Message.MessageString}");
    }
}