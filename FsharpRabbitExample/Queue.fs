[<AutoOpen>]
module Queue

    open System.IO
    open System.Runtime.Serialization.Formatters.Binary

    open RabbitMQ.Client
    open RabbitMQ.Client.Events

    // Named queues
    type Queue =
    | OrderReceived
    | OrderPaid
    | OrderFulfilled

    let private resolve queue =
        match queue with
        | OrderReceived -> "orders"
        | OrderPaid -> "payments"
        | OrderFulfilled -> "fulfillments"

    // Serialization
    let private toBytes message = 
        let formatter = BinaryFormatter()
        use stream = new MemoryStream()
        formatter.Serialize(stream, message)
        stream.ToArray()

    let private fromBytes<'T> (message : byte[]) =
        let formatter = BinaryFormatter()
        use stream = new MemoryStream()
        stream.Write(message, 0, message.Length)
        stream.Seek(0L, SeekOrigin.Begin) |> ignore
        unbox<'T>(formatter.Deserialize(stream))

    // Helpers
    let private declare (channel : IModel) queueName =
        channel.QueueDeclare(queueName, true, false, false, null)

    // Config
    let private host = "127.0.0.1"
    let private port = 5672
    let private userName = ""
    let private password = ""
    let private exchange = ""

    let private factory = ConnectionFactory(HostName = host, Port = port, UserName = userName, Password = password)    

    // Add a message to queue
    let enqueue queue message = 
        use connection = factory.CreateConnection()
        use model = connection.CreateModel()

        let queueName = 
            queue 
            |> resolve
            |> (fun qn -> declare model qn |> ignore ; qn)

        let serializedMessage = 
            message 
            |> toBytes

        model.BasicPublish(exchange, queueName, null, serializedMessage)

    // Start push-based subscription to queue
    let subscribe<'T> queue callback = 
        let connection = factory.CreateConnection()
        let model = connection.CreateModel()
        
        let queueName =
            queue
            |> resolve
            |> (fun qn -> declare model qn |> ignore ; qn)

        let consumer = EventingBasicConsumer(model)
        consumer.Received.Add((fun message -> 
            message.Body
            |> fromBytes<'T>
            |> callback
        ))

        model.BasicConsume(queueName, true, consumer) |> ignore
        
        (fun () -> 
            model.Close()
            connection.Close()
        )
