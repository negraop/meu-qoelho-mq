from __future__ import print_function

import asyncio
import logging

import grpc
import MeuQoelhoMQ_pb2
import MeuQoelhoMQ_pb2_grpc

# Aqui está um cliente usando Python, que utiliza o mesmo arquivo protobuff
# Os métodos estão comentados apenas para realização do teste em vídeo.
# Porém, ambas as funcionalidades estão desenvolvidas, basta apenas descomentá-las.

async def run():

    with grpc.insecure_channel("localhost:5001") as channel:
        stub = MeuQoelhoMQ_pb2_grpc.BrokerServiceStub(channel)

        # # Cria uma fila
        # reply = stub.CreateQueue(
        #     MeuQoelhoMQ_pb2.CreateQueueRequest(name="Queue1", type=MeuQoelhoMQ_pb2.SIMPLE),
        #     timeout=5.0
        # )
        # print(f"\n*** Response ***\nSuccess: {reply.success}\nMessage: {reply.message}\nIdQueue: {reply.idQueue}")

        # # Cria outra fila
        # reply2 = stub.CreateQueue(
        #     MeuQoelhoMQ_pb2.CreateQueueRequest(name="Queue2", type=MeuQoelhoMQ_pb2.MULTIPLE),
        #     timeout=5.0
        # )
        # print(f"\n*** Response ***\nSuccess: {reply2.success}\nMessage: {reply2.message}\nIdQueue: {reply2.idQueue}")

        # # Cria uma terceira fila
        # reply3 = stub.CreateQueue(
        #     MeuQoelhoMQ_pb2.CreateQueueRequest(name="Queue3", type=MeuQoelhoMQ_pb2.SIMPLE),
        #     timeout=5.0
        # )
        # print(f"\n*** Response ***\nSuccess: {reply3.success}\nMessage: {reply3.message}\nIdQueue: {reply3.idQueue}")

        # # Exclui a terceira fila
        # reply4 = stub.DeleteQueue(
        #     MeuQoelhoMQ_pb2.DeleteQueueRequest(name="Queue3"),
        #     timeout=5.0
        # )
        # print(f"\n*** Response ***\nSuccess: {reply4.success}\nMessage: {reply4.message}\nIdQueue: {reply4.idQueue}")

        # # Publica uma mensagem Unary RPC
        # reply5 = stub.PublishMessage(
        #     MeuQoelhoMQ_pb2.PublishMessageRequest(queueName="Queue2", messageString="Hello World!"),
        #     timeout=5.0
        # )
        # print(f"\n*** Response ***\nSuccess: {reply5.success}\nMessage: {reply5.message}\nIdMessage: {reply5.idMessage}")


        # Publica uma mensagem Unary RPC
        reply5 = stub.PublishMessage(
            MeuQoelhoMQ_pb2.PublishMessageRequest(queueName="Queue1", messageString="Hello World!"),
            timeout=5.0
        )
        print(f"\n*** Response ***\nSuccess: {reply5.success}\nMessage: {reply5.message}\nIdMessage: {reply5.idMessage}")

        # # Pega uma mensagem da fila
        # reply6 = stub.GetMessage(
        #     MeuQoelhoMQ_pb2.GetMessageRequest(queueName="Queue2"),
        #     timeout=5.0
        # )
        # print(f"\n*** Response ***\nSuccess: {reply6.success}\nResponseMessage: {reply6.responseMessage}\nMessageId: {reply6.message.idMessage}\nMessageContent: {reply6.message.messageString}")

        # # Retorna a lista de filas criadas, incluindo o tipo e o número de mensagens restantes
        # reply7 = stub.GetQueues(
        #     MeuQoelhoMQ_pb2.Empty(),
        #     timeout=5.0
        # )
        # print("\nFilas já criadas:")
        # for queue in reply7.queues:
        #     print(f"Name: {queue.name}, Type: {"Simple" if queue.type == 0 else "Multiple"}, MessagesCount: {queue.messagesCount}")
        

        # # Cria as tarefas de assinatura
        # subscription_tasks = [
        #     asyncio.create_task(subscribe_queue(stub, "Queue2", 1000000)),
        # ]

        # # Aguarda todas as tarefas de assinatura em paralelo
        # await asyncio.gather(*subscription_tasks)

async def subscribe_queue(stub, name, deadline):
    reply = stub.SubscribeQueue(
        MeuQoelhoMQ_pb2.SubscribeRequest(queueName=name), 
        timeout=deadline
    )

    try:
        for message in reply:
            print(f"\nMensagem recebida na fila {name}:\nSuccess: {message.success}\nResponseMessage: {message.responseMessage}\nMessageId: {message.message.idMessage}\nMessageContent: {message.message.messageString}")
    except grpc.RpcError as e:
        if e.code() == grpc.StatusCode.DEADLINE_EXCEEDED:
            print(f"Deadline excedido para a fila {name}.")
        else:
            print(f"Erro na fila {name}: {e}")


if __name__ == "__main__":
    logging.basicConfig()
    asyncio.run(run())