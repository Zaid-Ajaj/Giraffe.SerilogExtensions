namespace Giraffe.SerilogExtensions

open System
open System.Diagnostics
open Microsoft.AspNetCore.Http
open Serilog.Core
open Serilog.Events

/// Extracts and logs properties from the Response of the HttpContext after having executed the input HttpHandler.
type ResponseLogEnricher(context: HttpContext, config: SerilogConfig, stopwatch: Stopwatch, requestId: string) = 
    interface ILogEventEnricher with 
        member this.Enrich(logEvent: LogEvent, _: ILogEventPropertyFactory) = 
            let (Choser ignoredRequestFields) = config.IgnoredRequestFields
            let included field = not (List.exists ((=) ("Response." + field)) ignoredRequestFields)
            let anyOf xs = fun x -> List.exists ((=) x) xs 
            let ifEmptyThen y x =
                if String.IsNullOrWhiteSpace(x) 
                then y else x 
            stopwatch.Stop()
            stopwatch.ElapsedMilliseconds
            |> int
            |> Enrichers.eventProperty "Duration"
            |> logEvent.AddOrUpdateProperty

            if context.Request.Path.HasValue then
                context.Request.Path.Value
                |> Enrichers.eventProperty "Path"
                |> logEvent.AddOrUpdateProperty 

            requestId
            |> Enrichers.eventProperty "RequestId"
            |> logEvent.AddOrUpdateProperty

            Enrichers.eventProperty "Type" "Response"
            |> logEvent.AddOrUpdateProperty            

            if included "Method" then 
                context.Request.Method
                |> Enrichers.eventProperty "Method"
                |> logEvent.AddOrUpdateProperty

            if included "StatusCode" then 
                context.Response.StatusCode
                |> Enrichers.eventProperty "StatusCode"
                |> logEvent.AddOrUpdateProperty

            if included "ContentType" then 
                context.Response.ContentType
                |> ifEmptyThen ""
                |> Enrichers.eventProperty "ContentType"
                |> logEvent.AddOrUpdateProperty
            
            if included "ContentLength" && context.Response.ContentLength.HasValue then 
                context.Response.ContentLength.Value
                |> Enrichers.eventProperty "ContentLength"
                |> logEvent.AddOrUpdateProperty 
            
            ()