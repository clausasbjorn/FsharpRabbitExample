open System

// Object used as queue message
type Order = { OrderId : int }

[<EntryPoint>]
let main argv = 

    // Set up subscription
    let cancelOrderReceivedQueue = subscribe<Order> OrderReceived (fun message -> 
        printfn "Order received! Id #%d" message.OrderId // Message is strongly typed
    )

    // Create function for adding message to specific queue using partial application
    let enqueueOrder = (enqueue OrderReceived)

    // Keep looping until user quits
    let rec loop id =
        let char = Console.ReadKey()
        match char.Key with
        | ConsoleKey.Escape -> cancelOrderReceivedQueue()
        | _ ->
            enqueueOrder { OrderId = id }
            loop (id + 1)
    
    printfn "Press a key to order, [ESC] to exit"
    loop 0

    0