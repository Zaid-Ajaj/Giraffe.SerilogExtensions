module Tests

open Expecto
open Giraffe
open System.Linq
open System.Collections.Generic
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.TestHost
open Serilog.Sinks.TestCorrelator
open Giraffe.SerilogExtensions
open Serilog
open Serilog.Formatting.Json

let app =
  choose [ route "/index" >=> text "Index"
           route "/manyLogs" >=> context (fun ctx ->
                let logger = ctx.Logger()
                logger.Information("Read my {RequestId}")
                text "Many logs")

           text "Not Found" ]

let appWithLogger = SerilogAdapter.Enable(app)

Log.Logger <-
  LoggerConfiguration()
     .WriteTo.TestCorrelator()
     .WriteTo.Console()
     .WriteTo.Console(JsonFormatter())
     .CreateLogger()

let pass() = Expect.isTrue true "Passed"
let fail() = Expect.isTrue false "Failed"

let rnd = System.Random()


[<Tests>]
let tests =
  testList "Serilog Extensions" [

    testCase "Ingoring request fields" <| fun _ ->
      let ignoredFields =
        Ignore.fromRequest
        |> Field.path
        |> Field.method
        |> Field.host
        |> fun (Choser fields) -> fields
        |> Array.ofList

      Expect.equal "Request.Path" ignoredFields.[2] "Path is correct"
      Expect.equal "Request.Method" ignoredFields.[1] "Method is correct"
      Expect.equal "Request.Host" ignoredFields.[0] "Host is correct"

  ]
