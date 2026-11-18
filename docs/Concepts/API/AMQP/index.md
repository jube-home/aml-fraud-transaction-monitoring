---
layout: default
title: AMQP Queue Ingestion and Exchange Publishing
nav_order: 3
parent: API
grand_parent: Concepts
---

# AMQP Queue Ingestion and Exchange Publishing

In addition to the synchronous and asynchronous HTTP model invocation methods, there exists AMQP, which is available for processing the same exact payload specification. The model GUID (EntityAnalysisModelGuid) is passed in a header rather than the URL. There exists no Async switch as AMQP is asynchronous by its nature.

To enable AMQP integration, the Environment Variable must be configured to firstly allow AMQP, thereafter specify the fully qualified URL to establish a connection:

```text
AMQP=True
AMQPUri=amqps://<User>:<Password>@cow.rmq2.cloudamqp.com/User
```

AMQP is False by default, and AMQPUri has no default. Aside from the AMQP protocols, the messaging payload is identical to the HTTP API.

## Resources

Jube will establish queues and exchanges upon connection to the AMQP Server or Cluster. The resources required for AMQP integration are as follows:

| Resource     | Type     | Description                                                                                                                                                              |
|--------------|----------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| jubeInbound  | Queue    | A queue that accepts model request JSON in the body and a header titled EntityAnalysisModelGuid.                                                                         |
| jubeOutbound | Exchange | An exchange, of the fanout type, that accepts model response JSON in the body and is published such that queues may be subscribed to it.                                 |

As jubeOutbound is a fanout exchange, a queue bound to it without a routing key receives every model response. For the purposes of testing, it is a simple matter of creating a queue in the broker, for example one named jubeOutboundTest, and binding it to the jubeOutbound exchange, upon which it is subscribed to all messages passing through that exchange.

## Example

To pass a JSON message via AMQP, messages are queued to jubeInbound with JSON in the Body and a header named EntityAnalysisModelGuid which matches the model Guid (the EntityAnalysisModelGuid would otherwise have been part of the URL slug). The message to be published to the jubeInbound queue is as follows, the body being the same as that provided in `Http/Transaction.http` and abbreviated here:

```text
Queue:   jubeInbound
Headers: EntityAnalysisModelGuid = 90c425fd-101a-420b-91d1-cb7a24a969cc
```

```json
{
  "AccountId": "Test5",
  "TxnId": "09876543222",
  "TxnDateTime": "2001-08-01T00:57:39.248",
  "Currency": "AED",
  "ResponseCode": "0",
  "CurrencyAmount": "123.45",
  "IP": "123.456.789.123",
  "DeviceId": "OlaRoseGoldPhone5"
}
```

A message whose EntityAnalysisModelGuid header is missing, or is not a Guid, is dropped, there being no model to which it could be handed.

Queuing the message will make it available asynchronously to an instance of Jube with AMQP enabled. On receipt of a message from AMQP, it will be processed as if it were received synchronously, albeit sending the response payload to the jubeOutbound exchange, rather than as a response in HTTP context. It follows that a queue subscribed to the jubeOutbound exchange, as described above, receives a message whose body is the model response JSON, identical to that which would have been returned by HTTP, and abbreviated here:

```json
{
  "EntityAnalysisModelInstanceEntryGuid": "...",
  "ResponseElevation": { },
  "Payload": { }
}
```

{% comment %}TODO(verify): replace the abbreviated response above with a message captured from a queue bound to the jubeOutbound exchange on a running system with AMQP enabled{% endcomment %}
