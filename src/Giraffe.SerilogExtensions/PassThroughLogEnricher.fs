namespace Giraffe.SerilogExtensions

open System.Diagnostics
open Microsoft.AspNetCore.Http
open Serilog.Core
open Serilog.Events

type PassThroughLogEnricher(context: HttpContext, stopwatch: Stopwatch) = 
    interface ILogEventEnricher with 
        member this.Enrich(logEvent: LogEvent, _: ILogEventPropertyFactory) = 
            let anyOf xs = fun x -> List.exists ((=) x) xs 

            stopwatch.Stop()
            stopwatch.ElapsedMilliseconds
            |> int
            |> Enrichers.eventProperty "Duration"
            |> logEvent.AddOrUpdateProperty

            context.Items.Item "RequestId"
            |> unbox<string> 
            |> Enrichers.eventProperty "RequestId"
            |> logEvent.AddOrUpdateProperty
