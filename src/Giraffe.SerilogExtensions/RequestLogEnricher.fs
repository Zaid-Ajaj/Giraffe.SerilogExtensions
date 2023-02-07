namespace Giraffe.SerilogExtensions

open Microsoft.AspNetCore.Http
open Serilog.Core
open Serilog.Events
open System.IO
open System.Diagnostics
open System

/// Extracts and logs properties from the Request of the HttpContext after having executed the input HttpHandler.
type RequestLogEnricher(context: HttpContext, config: SerilogConfig, requestId: string) =
    interface ILogEventEnricher with
        member this.Enrich(logEvent: LogEvent, _: ILogEventPropertyFactory) =
            let (Choser ignoredRequestFields) = config.IgnoredRequestFields
            let included field = not (List.exists ((=) ("Request." + field)) ignoredRequestFields)
            let anyOf xs = fun x -> List.exists ((=) x) xs
            let ifEmptyThen y x =
                if String.IsNullOrWhiteSpace(x)
                then y else x

            requestId
            |> Enrichers.eventProperty "RequestId"
            |> logEvent.AddOrUpdateProperty

            Enrichers.eventProperty "Type" "Request"
            |> logEvent.AddOrUpdateProperty

            if included "Path" && context.Request.Path.HasValue then
                context.Request.Path.Value
                |> ifEmptyThen ""
                |> Enrichers.eventProperty "Path"
                |> logEvent.AddOrUpdateProperty
            elif included "Path" then
                Enrichers.eventProperty "Path" ""
                |> logEvent.AddOrUpdateProperty

            if included "FullPath" && context.Request.Path.HasValue && context.Request.QueryString.HasValue then
                context.Request.QueryString.Value
                |> sprintf "%s%s" context.Request.Path.Value
                |> ifEmptyThen ""
                |> Enrichers.eventProperty "FullPath"
                |> logEvent.AddOrUpdateProperty
            elif included "FullPath" && context.Request.Path.HasValue then
                context.Request.QueryString.Value
                |> sprintf "%s%s" context.Request.Path.Value
                |> ifEmptyThen ""
                |> Enrichers.eventProperty "FullPath"
                |> logEvent.AddOrUpdateProperty
            // if full path was included but it is not present, then leave it empty
            elif included "FullPath" then
                Enrichers.eventProperty "FullPath" ""
                |> logEvent.AddOrUpdateProperty

            if included "Method" then
                context.Request.Method
                |> Enrichers.eventProperty "Method"
                |> logEvent.AddOrUpdateProperty

            if included "Host" && context.Request.Host.HasValue then
                context.Request.Host.Host
                |> ifEmptyThen ""
                |> Enrichers.eventProperty "Host"
                |> logEvent.AddOrUpdateProperty

            if included "HostPort" && context.Request.Host.HasValue && context.Request.Host.Port.HasValue then
                context.Request.Host.Port.Value
                |> Enrichers.eventProperty "Port"
                |> logEvent.AddOrUpdateProperty

            if included "QueryString" && context.Request.QueryString.HasValue then
                context.Request.QueryString.Value
                |> Enrichers.eventProperty "QueryString"
                |> logEvent.AddOrUpdateProperty

            if included "Query" then
                let query = context.Request.Query
                let queryValues = [
                    for key in query.Keys ->
                        let values = Array.ofSeq (query.Item key)
                        key, String.concat ", " values
                ]

                queryValues
                |> Map.ofSeq
                |> Enrichers.eventProperty "Query"
                |> logEvent.AddOrUpdateProperty

            if included "Headers" then
                let headerValues = [
                    for key in context.Request.Headers.Keys ->
                        let value =
                            context.Request.Headers.[key]
                            |> Array.ofSeq
                            |> String.concat ", "

                        key, value
                ]

                headerValues
                |> List.filter (fun (key,_) -> not (List.exists ((=) key) config.IgnoredRequestHeaders))
                |> Map.ofList
                |> Enrichers.eventProperty "RequestHeaders"
                |> logEvent.AddOrUpdateProperty

            if included "UserAgent" then
                for key in context.Request.Headers.Keys do
                    if key = "User-Agent" || key = "user-agent" then
                        context.Request.Headers.[key]
                        |> Array.ofSeq
                        |> String.concat ", "
                        |> Enrichers.eventProperty "UserAgent"
                        |> logEvent.AddOrUpdateProperty

            if included "ContentType" then
                context.Request.ContentType
                |> ifEmptyThen ""
                |> Enrichers.eventProperty "ContentType"
                |> logEvent.AddOrUpdateProperty

            if included "ContentLength" && context.Request.ContentLength.HasValue then
                context.Request.ContentLength.Value
                |> Enrichers.eventProperty "ContentLength"
                |> logEvent.AddOrUpdateProperty
