# ZyRabbit

## Quick introduction
`ZyRabbit` is a modern .NET framework for communication over [RabbitMQ](http://rabbitmq.com/). The modular design and middleware oriented architecture makes the client highly customizable while providing sensible default for topology, routing and more. Documentation is currently found under [`/docs`](https://github.com/zylab-official/ZyRabbit/tree/master/docs).

### Configure, enrich and extend

`ZyRabbit` is configured with `ZyRabbitOptions`, an options object that makes it possible to register client configuration, plugins as well as override internal services

```csharp
var client = ZyRabbitFactory.CreateSingleton(new ZyRabbitOptions
{
  ClientConfiguration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("zyRabbit.json")
    .Build()
    .Get<ZyRabbitConfiguration>(),
  Plugins = p => p
    .UseProtobuf()
    .UsePolly(c => c
        .UsePolicy(queueBindPolicy, PolicyKeys.QueueBind)
        .UsePolicy(queueDeclarePolicy, PolicyKeys.QueueDeclare)
        .UsePolicy(exchangeDeclarePolicy, PolicyKeys.ExchangeDeclare)
    ),
  DependencyInjection = ioc => ioc
    .AddSingleton<IChannelFactory, CustomChannelFactory>()
});
```

### Publish/Subscribe
Set up strongly typed publish/subscribe in just a few lines of code.

```csharp
var client = ZyRabbitFactory.CreateSingleton();
await client.SubscribeAsync<BasicMessage>(async msg =>
{
  Console.WriteLine($"Received: {msg.Prop}.");
});

await client.PublishAsync(new BasicMessage { Prop = "Hello, world!"});
```

### Request/Response
`ZyRabbits` request/response (`RPC`) implementation uses the [direct reply-to feature](https://www.rabbitmq.com/direct-reply-to.html) for better performance and lower resource allocation.

```csharp
var client = ZyRabbitFactory.CreateSingleton();
client.RespondAsync<BasicRequest, BasicResponse>(async request =>
{
  return new BasicResponse();
});

var response = await client.RequestAsync<BasicRequest, BasicResponse>();
```

### Ack, Nack, Reject and Retry

Unlike many other clients, `basic.ack`, `basic.nack` and `basic.reject` are first class citizen in the message handler

```csharp
var client = ZyRabbitFactory.CreateSingleton();
await client.SubscribeAsync<BasicMessage>(async msg =>
{
  if(UnableToProcessMessage(msg))
  {
    return new Nack(requeue: true);
  }
  ProcessMessage(msg)
  return new Ack();
});
```

In addition to the basic acknowledgements, ZyRabbit also support delayed retries

```csharp
var client = ZyRabbitFactory.CreateSingleton();
await client.SubscribeAsync<BasicMessage>(async msg =>
{
  try
  {
    ProcessMessage(msg)
    return new Ack();
  }
  catch (Exception e)
  {
    return Retry.In(TimeSpan.FromSeconds(30));
  }
});
```

### Granular control for each call

Add or change properties in the `IPipeContext` to tailor calls for specific type of messages. This makes it possible to modify the topology features for calls, publish confirm timeout, consumer concurrency and much more

```csharp
await subscriber.SubscribeAsync<BasicMessage>(received =>
{
  receivedTcs.TrySetResult(received);
  return Task.FromResult(true);
}, ctx => ctx
  .UseSubscribeConfiguration(cfg => cfg
    .Consume(c => c
      .WithRoutingKey("custom_key")
      .WithConsumerTag("custom_tag")
      .WithPrefetchCount(2)
      .WithNoLocal(false))
    .FromDeclaredQueue(q => q
      .WithName("custom_queue")
      .WithAutoDelete()
      .WithArgument(QueueArgument.DeadLetterExchange, "dlx"))
    .OnDeclaredExchange(e=> e
      .WithName("custom_exchange")
      .WithType(ExchangeType.Topic))
));
```
