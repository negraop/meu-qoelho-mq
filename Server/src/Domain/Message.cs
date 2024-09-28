using Google.Protobuf;

namespace MeuQoelhoMQ.Domain;

public class Message
{
    public Guid Id { get; set; }
    public string? ContentString { get; set; }
    public ByteString? ContentBytes { get; set; }

    public Message(string content, ByteString contentBytes)
    {
        Id = Guid.NewGuid();
        ContentString = content;
        ContentBytes = contentBytes;
    }
}
