open System
open Suave
open Suave.Operators
open Suave.Successful
open Suave.RequestErrors
open Suave.Filters

let simpleApp =
    choose [
        GET >=> choose [
            path "/" >=> OK "Hello world"
        ]
        NOT_FOUND "404 - not found"
    ]

[<EntryPoint>]
let main argv = 
    startWebServer defaultConfig simpleApp
    0 // Integer-itcode zurückgeben
