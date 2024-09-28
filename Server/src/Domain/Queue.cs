using System.Collections.Concurrent;
using MeuQoelhoMQProto;
using src.Domain;
using src.Services;

namespace MeuQoelhoMQ.Domain;

public class Queue
{
    // ConcurrentQueue<T> é uma lib do .NET indicada para estruturas do tipo Fila 
    // (garante a ordem dos elementos) que precisam dessa segurança ao acessar 
    // recursos compartilhados.
    private ConcurrentQueue<Message> _messages = new ConcurrentQueue<Message>();
    private List<Subscriber> _subscribers = new List<Subscriber>();
    private int _nextSubscriberIndex = 0; // Indice do Round-robin
    private int _nextIndex = 0; // Indice das mensagens

    public Guid Id { get; set; }
    public string Name { get; set; }
    public QueueType Type { get; set; }
    public int MessagesCount 
    {
        get
        {
            return _messages.Count;
        }
    }

    public Queue(string name, QueueType type)
    {
        Id = Guid.NewGuid();
        Name = name;
        Type = type;
    }

    private void IncreaseIndexQueue()
    {
        _nextIndex++;
    }

    public int PublishMessage(Message message)
    {
        _messages.Enqueue(message);

        IncreaseIndexQueue();

        return _nextIndex;
    }

    public Message? GetMessage()
    {
        if (_messages.IsEmpty)
            return null;
        
        if (_messages.TryDequeue(out var message))
            return message;

        return null;
    }

    public void Subscribe(Subscriber subscriber)
    {
        lock (_subscribers)
        {
            _subscribers.Add(subscriber);
        }
    }

    public void Unsubscribe(Subscriber subscriber)
    {
        lock (_subscribers)
        {
            _subscribers.Remove(subscriber);
        }
    }

    public void NotifySubscribers()
    {
        List<Subscriber> subscribers;

        lock (_subscribers)
        {
            subscribers = _subscribers;
        }

        if (subscribers.Count == 0)
            return;

        Message? message = GetMessage();

        if (message == null)
            return;

        var reply = new GetMessageReply()
        {
            Success = true,
            ResponseMessage = $"Mensagem resgatada na fila '{Name}' com sucesso",
            Message = new MessageContent
            {
                IdMessage = message.Id.ToString(),
                QueueName = this.Name,
                MessageString = message.ContentString,
                MessageBytes = message.ContentBytes
            }
        };

        if (this.Type == QueueType.Simple)
            NotifySimple(subscribers, reply);
        else
            NotifyMultiple(subscribers, reply);

        BackupService.RemoveMessageFromDisk(Name, message.Id);
        
    }

    private void NotifySimple(List<Subscriber> subscribers, GetMessageReply message)
    {
        var subscriber = GetNextSubscriber();

        if (subscriber != null)
        {
            subscriber.Stream.WriteAsync(message);
        }
    }

    // Algoritmo de Round-Robin
    public Subscriber? GetNextSubscriber()
    {
        if (_subscribers.Count == 0)
            return null;

        lock (_subscribers)
        {
            var subscriber = _subscribers[_nextSubscriberIndex];

            _nextSubscriberIndex = (_nextSubscriberIndex + 1) % _subscribers.Count;

            return subscriber;
        }
    }

    private void NotifyMultiple(List<Subscriber> subscribers, GetMessageReply message)
    {
        foreach (var subscriber in subscribers)
        {
            subscriber.Stream.WriteAsync(message);
        }
    }
}
