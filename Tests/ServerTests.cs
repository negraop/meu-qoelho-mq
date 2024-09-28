using Google.Protobuf;
using MeuQoelhoMQProto;
using MeuQoelhoMQServer;

namespace Tests;

public class ServerTests
{
    [Fact]
    public void CreateQueue_ShouldAddQueue_WhenQueueDoesNotExist()
    {
        // Given
        var server = new Server();
        var request = new CreateQueueRequest() { Name = "queue1", Type = QueueType.Simple };

        // When
        var result = server.CreateQueue(request);

        // Then
        Assert.True(result.Success);
    }

    [Fact]
    public void CreateQueue_ShouldReturnError_WhenQueueExists()
    {
        // Given
        var server = new Server();
        var request = new CreateQueueRequest() { Name = "queue1", Type = QueueType.Simple };
        server.CreateQueue(request);

        // When
        var result = server.CreateQueue(request);

        // Then
        Assert.False(result.Success);
    }

    // Utilizei o ChatGPT para me ajudar na construção de alguns testes,
    // especificamente os que testam se os métodos são ThreadSafe

    [Fact]
    public async Task CreateQueue_ShouldBeThreadSafe()
    {
        // Arrange
        var server = new Server();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            int index = i;
            tasks.Add(Task.Run(() => 
            {
                var request = new CreateQueueRequest { Name = $"queue{index}", Type = QueueType.Simple };
                server.CreateQueue(request);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(100, server.QueuesCount);
        var queues = server.GetQueues().Queues;
        Assert.Equal(100, queues.Count);
        Assert.True(queues.All(q => q.Name.StartsWith("queue")));
    }

    [Fact]
    public void DeleteQueue_ShouldRemoveQueue_WhenQueueExists()
    {
        // Given
        var server = new Server();
        var request = new CreateQueueRequest() { Name = "queue1", Type = QueueType.Simple };
        server.CreateQueue(request);

        // When
        var result = server.DeleteQueue(request.Name);

        // Then
        Assert.True(result.Success);
    }

    [Fact]
    public void DeleteQueue_ShouldNotRemoveQueue_WhenExistsRemainingMessages()
    {
        // Given
        var server = new Server();
        var request = new CreateQueueRequest() { Name = "queue1", Type = QueueType.Simple };
        var messageRequest = new PublishMessageRequest() { QueueName = "queue1", MessageString = "Test" };
        server.CreateQueue(request);
        server.PublishMessage(messageRequest);

        // When
        var result = server.DeleteQueue(request.Name);

        // Then
        Assert.False(result.Success);
    }

    [Fact]
    public void DeleteQueue_ShouldReturnError_WhenQueueDoesNotExist()
    {
        // Given
        var server = new Server();
        var request = new CreateQueueRequest() { Name = "queue1", Type = QueueType.Simple };
        server.CreateQueue(request);

        // When
        var result = server.DeleteQueue("foobar");

        // Then
        Assert.False(result.Success);
    }

    [Fact]
    public async Task DeleteQueue_ShouldBeThreadSafe()
    {
        // Arrange
        var server = new Server();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            var request = new CreateQueueRequest { Name = $"queue{i}", Type = QueueType.Simple };
            server.CreateQueue(request);
        }

        for (int i = 0; i < 100; i++)
        {
            int index = i;
            tasks.Add(Task.Run(() => server.DeleteQueue($"queue{index}")));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(0, server.QueuesCount);
        var queues = server.GetQueues().Queues;
        Assert.Empty(queues);
    }

    [Fact]
    public void GetQueues_ShouldReturnAllQueues()
    {
        // Arrange
        var server = new Server();
        var request1 = new CreateQueueRequest { Name = "queue1", Type = QueueType.Simple };
        var request2 = new CreateQueueRequest { Name = "queue2", Type = QueueType.Multiple };
        server.CreateQueue(request1);
        server.CreateQueue(request2);

        // Act
        var result = server.GetQueues();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Queues.Count);
    }

    [Fact]
    public void GetQueues_ShouldReturnAllQueues_WithNameAndTypes()
    {
        // Arrange
        var server = new Server();
        var request1 = new CreateQueueRequest { Name = "queue1", Type = QueueType.Simple };
        var request2 = new CreateQueueRequest { Name = "queue2", Type = QueueType.Multiple };
        server.CreateQueue(request1);
        server.CreateQueue(request2);

        // Act
        var result = server.GetQueues();

        // Assert
        var queue1 = result.Queues.FirstOrDefault(q => q.Name == "queue1");
        var queue2 = result.Queues.FirstOrDefault(q => q.Name == "queue2");

        Assert.Contains(result.Queues, q => q.Name == "queue1");
        Assert.NotNull(queue1);
        Assert.Equal(QueueType.Simple, queue1.Type);
        
        Assert.Contains(result.Queues, q => q.Name == "queue2");
        Assert.NotNull(queue2);
        Assert.Equal(QueueType.Multiple, queue2.Type);
    }

    [Fact]
    public async Task GetQueues_ShouldBeThreadSafe()
    {
        // Arrange
        var server = new Server();
        var createTasks = new List<Task>();

        for (int i = 0; i < 50; i++)
        {
            int index = i;
            createTasks.Add(Task.Run(() => 
            {
                var request = new CreateQueueRequest { Name = $"queue{index}", Type = QueueType.Simple };
                server.CreateQueue(request);
            }));
        }

        await Task.WhenAll(createTasks);

        var getTasks = new List<Task<QueueListReply>>();
        for (int i = 0; i < 50; i++)
        {
            getTasks.Add(Task.Run(() => server.GetQueues()));
        }

        var results = await Task.WhenAll(getTasks);

        // Assert
        var allQueues = results.SelectMany(result => result.Queues).Distinct().ToList();
        Assert.Equal(50, allQueues.Count);
        Assert.True(allQueues.All(q => q.Name.StartsWith("queue")));
    }

    [Fact]
    public void PublishMessage_ShouldReturnError_WhenMessageStringOrMessageByteIsEmpty()
    {
        // Given
        var server = new Server();
        var request = new CreateQueueRequest() { Name = "queue1", Type = QueueType.Simple };
        var messageRequest = new PublishMessageRequest() { QueueName = "queue1" };
        server.CreateQueue(request);

        // When
        var result = server.PublishMessage(messageRequest);

        // Then
        Assert.False(result.Success);
    }

    [Fact]
    public void PublishMessage_ShouldReturnError_WhenQueueDoesNotExist()
    {
        // Given
        var server = new Server();
        var request = new CreateQueueRequest() { Name = "queue1", Type = QueueType.Simple };
        var messageRequest = new PublishMessageRequest() { QueueName = "queue2" };
        server.CreateQueue(request);

        // When
        var result = server.PublishMessage(messageRequest);

        // Then
        Assert.False(result.Success);
    }

    [Fact]
    public void PublishMessage_ShouldPublishMessageString_WhenQueueExists()
    {
        // Given
        var server = new Server();
        var request = new CreateQueueRequest() { Name = "queue1", Type = QueueType.Simple };
        var messageRequest = new PublishMessageRequest() { QueueName = "queue1", MessageString = "Test" };
        server.CreateQueue(request);

        // When
        var result = server.PublishMessage(messageRequest);

        // Then
        Assert.True(result.Success);
    }

    [Fact]
    public void PublishMessage_ShouldPublishMessageBytes_WhenQueueExists()
    {
        // Given
        var server = new Server();
        var request = new CreateQueueRequest() { Name = "queue1", Type = QueueType.Simple };
        var messageRequest = new PublishMessageRequest() { QueueName = "queue1", MessageBytes = ByteString.CopyFromUtf8("Test") };
        server.CreateQueue(request);

        // When
        var result = server.PublishMessage(messageRequest);

        // Then
        Assert.True(result.Success);
    }

    [Fact]
    public void PublishMessages_ShouldPublishAllMessage()
    {
        // Given
        var server = new Server();
        var request = new CreateQueueRequest() { Name = "queue1", Type = QueueType.Simple };
        server.CreateQueue(request);
        var messages = new List<PublishMessageRequest>()
        {
            new PublishMessageRequest() { QueueName = "queue1", MessageString = "Test1" },
            new PublishMessageRequest() { QueueName = "queue1", MessageString = "Test2" },
            new PublishMessageRequest() { QueueName = "queue1", MessageString = "Test3" },
            new PublishMessageRequest() { QueueName = "queue1", MessageString = "Test4" },
            new PublishMessageRequest() { QueueName = "queue1", MessageString = "Test5" },
        };
        var messagesRequest = new PublishMessagesRequest();
        messagesRequest.Messages.AddRange(messages);
        
        // When
        var result = server.PublishMessages(messagesRequest);

        // Then:
        Assert.True(result.Success);

        var queueListResponse = server.GetQueues();
        var queue = queueListResponse.Queues.FirstOrDefault(q => q.Name == "queue1");

        Assert.NotNull(queue);
        Assert.Equal(5, queue.MessagesCount);
    }
}