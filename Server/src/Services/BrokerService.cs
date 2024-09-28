using Grpc.Core;
using MeuQoelhoMQProto;

namespace MeuQoelhoMQServer.Services;

public class BrokerServiceImpl : BrokerService.BrokerServiceBase
{
    // Outra dificuldade que enfrentei foi que eu estava implementando um único arquivo
    // para o servidor, e esse arquivo herdava do QueueServiceBase. Enquanto estava 
    // implementando a lógica das filas de mensagens (um único service do Protobuff) 
    // estava tudo funcionando perfeitamente. Quando eu fui implementar a lógica das
    // mensagens (outro service do Protobuff), esbarrei numa limitação da linguagem C#:
    // ela não permite herdar de mais de uma classe base (=/)!
    // Por conta disso eu preferi criar um único service no ProtoBuff chamado BrokerService
    // que contém todos os métodos relacionados ao projeto.

    private readonly Server _server;
    public BrokerServiceImpl(Server server)
    {
        _server = server;
    }

    public override async Task<QueueReply> CreateQueue(CreateQueueRequest request, ServerCallContext context)
    {
        // Adicionei um atraso propositalmente para testar a funcionalidade de Deadline
        await Task.Delay(50);

        Console.WriteLine($"\nCliente: {context.Peer}");
        Console.WriteLine($"Metodo: {context.Method}");
        Console.WriteLine($"Nome: {request.Name}, Tipo: {request.Type}");

        return await Task.FromResult(_server.CreateQueue(request));
    }

    public override async Task<QueueReply> DeleteQueue(DeleteQueueRequest request, ServerCallContext context)
    {
        Console.WriteLine($"\nCliente: {context.Peer}");
        Console.WriteLine($"Metodo: {context.Method}");
        Console.WriteLine($"Nome: {request.Name}");

        return await Task.FromResult(_server.DeleteQueue(request.Name));
    }

    public override async Task<QueueListReply> GetQueues(Empty request, ServerCallContext context)
    {
        Console.WriteLine($"\nCliente: {context.Peer}");
        Console.WriteLine($"Metodo: {context.Method}");
        Console.WriteLine("\nGET - QTD de Filas: " + _server.QueuesCount);
        
        return await Task.FromResult(_server.GetQueues());
    }

    public override async Task<MessageReply> PublishMessage(PublishMessageRequest request, ServerCallContext context)
    {
        Console.WriteLine($"\nCliente: {context.Peer}");
        Console.WriteLine($"Metodo: {context.Method}");
        Console.WriteLine($"Nome: {request.QueueName}");

        return await Task.FromResult(_server.PublishMessage(request));
    }

    public override async Task<MessagesReply> PublishMessages(PublishMessagesRequest request, ServerCallContext context)
    {
        Console.WriteLine($"\nCliente: {context.Peer}");
        Console.WriteLine($"Metodo: {context.Method}");

        return await Task.FromResult(_server.PublishMessages(request));
    }

    public override async Task<GetMessageReply> GetMessage(GetMessageRequest request, ServerCallContext context)
    {
        Console.WriteLine($"\nCliente: {context.Peer}");
        Console.WriteLine($"Metodo: {context.Method}");
        Console.WriteLine($"Nome: {request.QueueName}");

        return await Task.FromResult(_server.GetMessage(request));
    }

    public override async Task SubscribeQueue(SubscribeRequest request, IServerStreamWriter<GetMessageReply> responseStream, ServerCallContext context)
    {
        Console.WriteLine($"\nCliente: {context.Peer}");
        Console.WriteLine($"Metodo: {context.Method}");
        Console.WriteLine($"Nome: {request.QueueName}");

        await _server.Subscribe(request, responseStream, context);
    }
}
