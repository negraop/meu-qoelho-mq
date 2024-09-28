using Google.Protobuf;
using MeuQoelhoMQ.Domain;
using MeuQoelhoMQProto;

namespace src.Services;

public class BackupService
{
    private List<Queue> _queues = new List<Queue>();

    public static void SaveMessageInDisk(PublishMessageRequest request, Guid id, int index, QueueType queueType)
    {
        string backupDirectory = "./Backup";
        
        if (!Directory.Exists(backupDirectory))
        {
            Directory.CreateDirectory(backupDirectory);
        }

        string fileName = $"{request.QueueName}_{queueType}_{index}_{id}.bin";
        string filePath = Path.Combine(backupDirectory, fileName);

        // Na documentação do protobuf tem um exemplo de como Serializar e Desserializar
        // usando C#: https://protobuf.dev/getting-started/csharptutorial/
        using (var fileStream = File.Create(filePath))
        {
            using (var codedOutputStream = new CodedOutputStream(fileStream))
            {
                request.WriteTo(codedOutputStream);
            }
        }
    }

    public static void RemoveMessageFromDisk(string queueName, Guid id)
    {
        string backupDirectory = "./Backup";
        
        if (!Directory.Exists(backupDirectory))
        {
            Console.WriteLine("Diretório de backup não existe.");
            return;
        }

        string[] files = Directory.GetFiles(backupDirectory, $"{queueName}_*_{id}.bin");

        if (files.Length == 0)
        {
            Console.WriteLine("Arquivo não encontrado.");
            return;
        }

        string filePath = files[0];

        try
        {
            File.Delete(filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao tentar remover o arquivo: {ex.Message}");
        }
    }

    // Confesso que demorei para entender como funcionaria a restauração das mensagens
    // quando o servidor for reerguido. Mas ver ele funcionando é muito lindo =D
    public List<Queue> RestoreQueuesFromBackup()
    {
        string backupDirectory = "./Backup";

        if (!Directory.Exists(backupDirectory))
        {
            return new List<Queue>();
        }

        var files = Directory.GetFiles(backupDirectory, "*.bin");

        // Ordenando os arquivos pelo nome da fila e pelo índice da mensagem
        // Usei o ChatGPT para me ajudar a ordenar os arquivos, eles estavam vindo em ordem aleatória
        var orderedFiles = files
            .Select(file => new
            {
                FileName = file,
                QueueName = Path.GetFileNameWithoutExtension(file).Split('_')[0],
                MessageIndex = int.Parse(Path.GetFileNameWithoutExtension(file).Split('_')[2])
            })
            .OrderBy(f => f.QueueName)
            .ThenBy(f => f.MessageIndex)
            .Select(f => f.FileName)
            .ToList();

        foreach (var file in orderedFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var parts = fileName.Split('_');

            // Verifica se o nome do arquivo tem as 4 partes (nome da fila, tipo, indice da mensagem e id da mensagem)
            if (parts.Length != 4) continue;

            string queueName = parts[0];
            QueueType queueType = parts[1] == "Simple" ? QueueType.Simple : QueueType.Multiple;
            int i = int.Parse(parts[2]);
            Guid messageId = Guid.Parse(parts[3]);

            Console.WriteLine("Fila: " + queueName + ", Tipo: " + queueType + ", Indice: " + i + ", Id: " + messageId);

            using (var input = File.OpenRead(file))
            {
                // o gRPC facilita demais a desserialização do objeto =)
                var messageRequest = PublishMessageRequest.Parser.ParseFrom(input);

                Console.WriteLine("Mensagem Resgatada: " + messageRequest);

                Queue? queue = _queues.FirstOrDefault(x => x.Name == queueName);

                if (queue == null)
                {
                    // Cria a nova fila
                    queue = new Queue(queueName, queueType);
                    _queues.Add(queue);
                }

                var message = new Message(messageRequest.MessageString, messageRequest.MessageBytes)
                {
                    Id = messageId,
                };

                // if (string.IsNullOrWhiteSpace(messageRequest.MessageString))
                //     message.ContentBytes = messageRequest.MessageBytes;
                // else
                //     message.ContentString = messageRequest.MessageString;

                // Publica a mensagem na fila
                var index = queue?.PublishMessage(message);
                Console.WriteLine(index);

                // Renomeia o arquivo com o indice atualizado
                string newFileName = $"{queueName}_{queueType}_{index}_{messageId}.bin";
                string newFilePath = Path.Combine(backupDirectory, newFileName);

                if (!File.Exists(newFilePath))
                    File.Move(file, newFilePath);
            }
        }

        return _queues;
    }
}
