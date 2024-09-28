from google.protobuf.internal import containers as _containers
from google.protobuf.internal import enum_type_wrapper as _enum_type_wrapper
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from typing import ClassVar as _ClassVar, Iterable as _Iterable, Mapping as _Mapping, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class QueueType(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    SIMPLE: _ClassVar[QueueType]
    MULTIPLE: _ClassVar[QueueType]
SIMPLE: QueueType
MULTIPLE: QueueType

class Empty(_message.Message):
    __slots__ = ()
    def __init__(self) -> None: ...

class CreateQueueRequest(_message.Message):
    __slots__ = ("name", "type")
    NAME_FIELD_NUMBER: _ClassVar[int]
    TYPE_FIELD_NUMBER: _ClassVar[int]
    name: str
    type: QueueType
    def __init__(self, name: _Optional[str] = ..., type: _Optional[_Union[QueueType, str]] = ...) -> None: ...

class DeleteQueueRequest(_message.Message):
    __slots__ = ("name",)
    NAME_FIELD_NUMBER: _ClassVar[int]
    name: str
    def __init__(self, name: _Optional[str] = ...) -> None: ...

class QueueReply(_message.Message):
    __slots__ = ("success", "message", "idQueue")
    SUCCESS_FIELD_NUMBER: _ClassVar[int]
    MESSAGE_FIELD_NUMBER: _ClassVar[int]
    IDQUEUE_FIELD_NUMBER: _ClassVar[int]
    success: bool
    message: str
    idQueue: str
    def __init__(self, success: bool = ..., message: _Optional[str] = ..., idQueue: _Optional[str] = ...) -> None: ...

class QueueSummary(_message.Message):
    __slots__ = ("idQueue", "type", "name", "messagesCount")
    IDQUEUE_FIELD_NUMBER: _ClassVar[int]
    TYPE_FIELD_NUMBER: _ClassVar[int]
    NAME_FIELD_NUMBER: _ClassVar[int]
    MESSAGESCOUNT_FIELD_NUMBER: _ClassVar[int]
    idQueue: str
    type: QueueType
    name: str
    messagesCount: int
    def __init__(self, idQueue: _Optional[str] = ..., type: _Optional[_Union[QueueType, str]] = ..., name: _Optional[str] = ..., messagesCount: _Optional[int] = ...) -> None: ...

class QueueListReply(_message.Message):
    __slots__ = ("success", "queues")
    SUCCESS_FIELD_NUMBER: _ClassVar[int]
    QUEUES_FIELD_NUMBER: _ClassVar[int]
    success: bool
    queues: _containers.RepeatedCompositeFieldContainer[QueueSummary]
    def __init__(self, success: bool = ..., queues: _Optional[_Iterable[_Union[QueueSummary, _Mapping]]] = ...) -> None: ...

class PublishMessageRequest(_message.Message):
    __slots__ = ("queueName", "messageString", "messageBytes")
    QUEUENAME_FIELD_NUMBER: _ClassVar[int]
    MESSAGESTRING_FIELD_NUMBER: _ClassVar[int]
    MESSAGEBYTES_FIELD_NUMBER: _ClassVar[int]
    queueName: str
    messageString: str
    messageBytes: bytes
    def __init__(self, queueName: _Optional[str] = ..., messageString: _Optional[str] = ..., messageBytes: _Optional[bytes] = ...) -> None: ...

class PublishMessagesRequest(_message.Message):
    __slots__ = ("messages",)
    MESSAGES_FIELD_NUMBER: _ClassVar[int]
    messages: _containers.RepeatedCompositeFieldContainer[PublishMessageRequest]
    def __init__(self, messages: _Optional[_Iterable[_Union[PublishMessageRequest, _Mapping]]] = ...) -> None: ...

class MessageReply(_message.Message):
    __slots__ = ("success", "message", "idMessage")
    SUCCESS_FIELD_NUMBER: _ClassVar[int]
    MESSAGE_FIELD_NUMBER: _ClassVar[int]
    IDMESSAGE_FIELD_NUMBER: _ClassVar[int]
    success: bool
    message: str
    idMessage: str
    def __init__(self, success: bool = ..., message: _Optional[str] = ..., idMessage: _Optional[str] = ...) -> None: ...

class MessagesReply(_message.Message):
    __slots__ = ("success", "messagesResponse")
    SUCCESS_FIELD_NUMBER: _ClassVar[int]
    MESSAGESRESPONSE_FIELD_NUMBER: _ClassVar[int]
    success: bool
    messagesResponse: _containers.RepeatedCompositeFieldContainer[MessageReply]
    def __init__(self, success: bool = ..., messagesResponse: _Optional[_Iterable[_Union[MessageReply, _Mapping]]] = ...) -> None: ...

class GetMessageRequest(_message.Message):
    __slots__ = ("queueName",)
    QUEUENAME_FIELD_NUMBER: _ClassVar[int]
    queueName: str
    def __init__(self, queueName: _Optional[str] = ...) -> None: ...

class MessageContent(_message.Message):
    __slots__ = ("idMessage", "queueName", "messageString", "messageBytes")
    IDMESSAGE_FIELD_NUMBER: _ClassVar[int]
    QUEUENAME_FIELD_NUMBER: _ClassVar[int]
    MESSAGESTRING_FIELD_NUMBER: _ClassVar[int]
    MESSAGEBYTES_FIELD_NUMBER: _ClassVar[int]
    idMessage: str
    queueName: str
    messageString: str
    messageBytes: bytes
    def __init__(self, idMessage: _Optional[str] = ..., queueName: _Optional[str] = ..., messageString: _Optional[str] = ..., messageBytes: _Optional[bytes] = ...) -> None: ...

class GetMessageReply(_message.Message):
    __slots__ = ("success", "responseMessage", "message")
    SUCCESS_FIELD_NUMBER: _ClassVar[int]
    RESPONSEMESSAGE_FIELD_NUMBER: _ClassVar[int]
    MESSAGE_FIELD_NUMBER: _ClassVar[int]
    success: bool
    responseMessage: str
    message: MessageContent
    def __init__(self, success: bool = ..., responseMessage: _Optional[str] = ..., message: _Optional[_Union[MessageContent, _Mapping]] = ...) -> None: ...

class SubscribeRequest(_message.Message):
    __slots__ = ("queueName",)
    QUEUENAME_FIELD_NUMBER: _ClassVar[int]
    queueName: str
    def __init__(self, queueName: _Optional[str] = ...) -> None: ...
