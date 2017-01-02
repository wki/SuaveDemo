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

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<Arguments>()
    let results = parser.Parse argv
    let nrRequests = results.GetResult(<@ Requests @>, defaultValue = 100)
    let nrThreads = results.GetResult(<@ Threads @>, defaultValue = 1)
    let uri = new Uri(results.GetResult(<@ Uri @>))

    printfn "Calling %A %d times, %d thread(s)" uri nrRequests nrThreads

    let executionTimes =
        uri
        |> Seq.replicate nrRequests
        |> Seq.map download
        |> Seq.map measureTime
        |> Array.ofSeq
    
    printfn "Min: %0.2fms, Avg: %0.2fms, Max: %0.2fms" (Array.min(executionTimes)) (Array.average(executionTimes)) (Array.max(executionTimes))

    0 // Integer-Exitcode zurückgeben
