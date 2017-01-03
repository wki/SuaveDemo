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
    | Begin of System.Uri * int * AsyncReplyChannel<TimeSpan list>
    | WantWork of AsyncReplyChannel<Uri>
    | DoneWork of TimeSpan

type DispatchState = {
    url: Uri
    todo: int
    toreceive: int
    executionTimes: TimeSpan list
    channel: AsyncReplyChannel<TimeSpan list> option
    lastProgressReported: DateTime
}

let dispatchActor (inbox: MailboxProcessor<DispatchMsg>) =
    let progressDelay = TimeSpan.FromSeconds(2.0)

    let rec loop(state) = async {
        let! msg = inbox.Receive()
        match msg with
        | Begin(uri, todo, r) ->
            // printfn "Begin: %d calls" todo
            return! loop({state with url = uri; todo = todo; toreceive = todo; executionTimes = []; channel = Some r})
        | WantWork replyChannel when state.todo > 0  ->
            // printfn "Someone wants work"
            replyChannel.Reply(state.url)
            return! loop({ state with todo = state.todo - 1})
        | DoneWork timespan ->
            let stillToreceive = state.toreceive - 1
            let lastProgressReported =
                if (state.lastProgressReported < DateTime.Now - progressDelay)
                then
                   let total = state.toreceive + List.length(state.executionTimes)
                   let received = total - state.toreceive
                   let percentage = 100 * received / total

                   printfn "Received %d of %d (%d%%)" received total percentage 
                   DateTime.Now
                else
                   state.lastProgressReported
            // printfn "Done within %0.1fms, wait for %d" (timespan.TotalMilliseconds) stillToreceive
            return! loop({state with toreceive = stillToreceive; executionTimes = timespan :: state.executionTimes; lastProgressReported = lastProgressReported})
        | _ when state.toreceive = 0 ->
            // printfn "quitting"
            state.channel.Value.Reply(state.executionTimes) 
            return ()
        | _ ->
            // printf "ignoring"
            return! loop(state)
    }
    loop({ url = null; todo = 0; toreceive = 0; executionTimes = []; channel = None; lastProgressReported = DateTime.Now })

let dispatchAgent = new MailboxProcessor<DispatchMsg>(dispatchActor)

// -- downloading Agent
let buildDownloadAgent(i:int) = new MailboxProcessor<System.Uri>(fun inbox -> 
    let rec loop(nr) = async {
        let uri = dispatchAgent.PostAndReply(fun replyChannel -> WantWork replyChannel)

        // printfn "about to download %A - Agent %d Thread %d" uri nr System.Threading.Thread.CurrentThread.ManagedThreadId

        let startTime = DateTime.Now
        let! result = download uri
        let duration = DateTime.Now - startTime
        dispatchAgent.Post(DoneWork(duration))

        return! loop(nr)
    }

    // printfn "Starting DownloadAgent %d. Thread Id: %d" i System.Threading.Thread.CurrentThread.ManagedThreadId
    loop(i))

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<Arguments>()
    let results = parser.Parse argv
    let nrRequests = results.GetResult(<@ Requests @>, defaultValue = 100)
    let nrThreads = results.GetResult(<@ Threads @>, defaultValue = 1)
    let uri = new Uri(results.GetResult(<@ Uri @>))

    printfn "Calling %A %d times, %d thread(s)" uri nrRequests nrThreads

    dispatchAgent.Start()
    let downloadAgents = [1..nrThreads] |> List.map (fun i -> buildDownloadAgent(i))
    downloadAgents |> List.iter (fun agent -> agent.Start())

    let executionTimes = List.map(fun (t:TimeSpan) -> t.TotalMilliseconds) (dispatchAgent.PostAndReply(fun r -> Begin(uri, nrRequests, r)))

    printfn "Min: %0.2fms, Avg: %0.2fms, Max: %0.2fms" (List.min(executionTimes)) (List.average(executionTimes)) (List.max(executionTimes))

    0 // Integer-Exitcode zurückgeben
