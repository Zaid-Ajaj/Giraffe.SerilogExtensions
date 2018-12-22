namespace Giraffe.SerilogExtensions

open System
open System.Diagnostics
open Giraffe
open Serilog.Core
open Serilog.Events
open Microsoft.AspNetCore.Http
    
type ErrorLogEnricher(context: HttpContext, stopwatch: Stopwatch, requestId: string) = 
    interface ILogEventEnricher with 
        member this.Enrich(logEvent: LogEvent, _: ILogEventPropertyFactory) = 
            let anyOf xs = fun x -> List.exists ((=) x) xs 
            let ifEmptyThen y x =
                if String.IsNullOrWhiteSpace(x) 
                then y else x 
            stopwatch.Stop()
            
            stopwatch.ElapsedMilliseconds
            |> int
            |> Enrichers.eventProperty "Duration"
            |> logEvent.AddOrUpdateProperty

            requestId
            |> Enrichers.eventProperty "RequestId"
            |> logEvent.AddOrUpdateProperty    

            context.Request.Method
            |> Enrichers.eventProperty "Method"
            |> logEvent.AddOrUpdateProperty

            if context.Request.Path.HasValue then
                context.Request.Path.Value
                |> ifEmptyThen ""
                |> Enrichers.eventProperty "Path"
                |> logEvent.AddOrUpdateProperty 

            Enrichers.eventProperty "StatusCode" 500
            |> logEvent.AddOrUpdateProperty

            Enrichers.eventProperty "Reason" "Internal Server Error"
            |> logEvent.AddOrUpdateProperty

            Enrichers.eventProperty "Type" "ServerError"
            |> logEvent.AddOrUpdateProperty
