open System
open Argu
open FSharp.Configuration

type Settings = AppSettings<"app.config">

[<HelpFlags("--help", "-h")>]
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

// --- dispatching Agent
type Statistic = TimeSpan
type DispatchMsg =
    | Begin of System.Uri * int * AsyncReplyChannel<Statistic list>
    | WantWork of AsyncReplyChannel<Uri>
    | DoneWork of Statistic

type DispatchState = {
    uri: Uri
    todo: int
    toreceive: int
    executionTimes: Statistic list
    channel: AsyncReplyChannel<Statistic list> option
    lastProgressReported: DateTime
}

let defaultState = { 
    uri = null
    todo = 0
    toreceive = 0
    executionTimes = []
    channel = None
    lastProgressReported = DateTime.Now
}

let dispatchActor (inbox: MailboxProcessor<DispatchMsg>) =
    let progressDelay = TimeSpan.FromSeconds(2.0)

    let rec loop(state) = async {
        let! msg = inbox.Receive()
        match msg with
        | Begin(uri, todo, r) ->
            return! loop({state with uri = uri; todo = todo; toreceive = todo; executionTimes = []; channel = Some r})
        | WantWork replyChannel when state.todo > 0  ->
            replyChannel.Reply(state.uri)
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
            return! loop({state with toreceive = stillToreceive; executionTimes = timespan :: state.executionTimes; lastProgressReported = lastProgressReported})
        | _ when state.toreceive = 0 ->
            state.channel.Value.Reply(state.executionTimes) 
            return ()
        | _ ->
            return! loop(state)
    }
    loop(defaultState)

let dispatchAgent = new MailboxProcessor<DispatchMsg>(dispatchActor)

// -- downloading Agent
let buildDownloadAgent(i:int) = new MailboxProcessor<System.Uri>(fun inbox ->
    let receiveUri() =
        dispatchAgent.PostAndReply(fun replyChannel -> WantWork replyChannel)

    let download = (new System.Net.WebClient()).AsyncDownloadString

    let measureTime task : Statistic =
        let startTime = DateTime.Now
        Async.RunSynchronously task |> ignore
        DateTime.Now - startTime

    let issueHttpRequest = download >> measureTime

    let reportStatistics = DoneWork >> dispatchAgent.Post

    let rec loop(nr) = async {
        receiveUri()
        |> issueHttpRequest
        |> reportStatistics

        return! loop(nr)
    }

    loop(i))

let startDownloadAgent (agent: MailboxProcessor<Uri>) =
    agent.Start()
    agent

[<EntryPoint>]
let main argv =
    let foo = Settings.Foo
    let bar = Settings.Bar
    printfn "Settings foo=%s, bar=%s" foo bar

    let errorHandler = ProcessExiter(colorizer = function ErrorCode.HelpText -> None | _ -> Some ConsoleColor.Red)
    let parser = ArgumentParser.Create<Arguments>(errorHandler = errorHandler)
    let results = parser.Parse argv
    let nrRequests = results.GetResult(<@ Requests @>, defaultValue = 100)
    let nrThreads = results.GetResult(<@ Threads @>, defaultValue = 1)
    let uri = new Uri(results.GetResult(<@ Uri @>))

    printfn "Calling %A %d times, %d thread(s)" uri nrRequests nrThreads

    dispatchAgent.Start()
    let downloadAgents = [1..nrThreads] |> List.map (buildDownloadAgent >> startDownloadAgent)

    let executionTimes = List.map(fun (t:TimeSpan) -> t.TotalMilliseconds) (dispatchAgent.PostAndReply(fun r -> Begin(uri, nrRequests, r)))

    printfn "Min: %0.2fms, Avg: %0.2fms, Max: %0.2fms" (List.min(executionTimes)) (List.average(executionTimes)) (List.max(executionTimes))

    0
