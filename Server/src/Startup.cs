using MeuQoelhoMQServer;
using MeuQoelhoMQServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddSingleton<Server>(); // Essa linha que me fez quebrar a cabeça por 1 dia inteiro.

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

// Restaura as filas do Backup, caso exista
var server = app.Services.GetRequiredService<Server>();
server.RestoreQueuesFromBackup();

app.MapGrpcService<BrokerServiceImpl>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
