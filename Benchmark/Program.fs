open System
open Argu

type Arguments =
    | [<AltCommandLine("-n")>] Requests of int
    | [<AltCommandLine("-c")>] Threads of int
    | [<Mandatory>] Uri of string
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Requests _ -> "nr of total requests"
            | Threads _ -> "nr of parallel threads"
            | Uri _ -> "uri to call"

// Uri -> Async<string>
let download uri =
    let webClient = new System.Net.WebClient()
    webClient.AsyncDownloadString(uri)

let measureTime task =
    let startTime = DateTime.Now
    Async.RunSynchronously task |> ignore
    (DateTime.Now - startTime).TotalMilliseconds

// --- dispatching Agent
type DispatchMsg =
    | Begin of System.Uri * int
    | WantWork of AsyncReplyChannel<Uri>
    | DoneWork of TimeSpan

let dispatchActor (inbox: MailboxProcessor<DispatchMsg>) =
    let rec loop(url, todo, toreceive) = async {
        let! msg = inbox.Receive()
        match msg with
        | Begin(uri, todo) ->
            printfn "Begin: %d calls" todo
            return! loop(uri, todo, todo)
        | WantWork replyChannel when todo > 0  ->
            printfn "Someone wants work"
            replyChannel.Reply(url)
            return! loop(url, todo-1, toreceive)
        | DoneWork timespan ->
            //  TODO: process
            let stillToreceive = toreceive - 1
            printfn "Done within %0.1fms, wait for %d" (timespan.TotalMilliseconds) stillToreceive
            return! loop(url, todo, stillToreceive)
        | x ->
            printfn "ignoring: %A" x 
            return! loop(url, todo, toreceive)
    }
    loop(null, 0, 0)

let dispatchAgent = new MailboxProcessor<DispatchMsg>(dispatchActor)

// -- downloading Agent
let downloadAgent = new MailboxProcessor<System.Uri>(fun inbox -> 
    let rec loop() = async {
        // dispatchAgent.Post(WantWork)
        dispatchAgent.PostAndReply(fun replyChannel -> WantWork replyChannel)

        let! uri = inbox.Receive()
        printfn "about to download %A" uri

        let startTime = DateTime.Now
        let! result = download uri
        let duration = DateTime.Now - startTime
        dispatchAgent.Post(DoneWork(duration))

        return! loop()
    }

    loop())

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<Arguments>()
    let results = parser.Parse argv
    let nrRequests = results.GetResult(<@ Requests @>, defaultValue = 100)
    let nrThreads = results.GetResult(<@ Threads @>, defaultValue = 1)
    let uri = new Uri(results.GetResult(<@ Uri @>))

    printfn "Calling %A %d times, %d thread(s)" uri nrRequests nrThreads

    // so sollte das gehen
    // TODO: n DownloadAgents starten 
    dispatchAgent.Start()
    dispatchAgent.Post(Begin(uri, nrRequests))
    downloadAgent.Start()

    System.Threading.Thread.Sleep(10000);

    let executionTimes =
        uri
        |> Seq.replicate nrRequests
        |> Seq.map (download >> measureTime)
        |> Array.ofSeq
    
    printfn "Min: %0.2fms, Avg: %0.2fms, Max: %0.2fms" (Array.min(executionTimes)) (Array.average(executionTimes)) (Array.max(executionTimes))

    0 // Integer-Exitcode zurückgeben
