namespace Giraffe.SerilogExtensions

open System
open System.Diagnostics
open Giraffe
open Serilog
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks
open System.Threading.Tasks

[<AutoOpen>]
module Extensions =
    type HttpContext with
        /// Returns a logger with the RequestId attached to it, if present.
        member ctx.Logger() : ILogger =
            if ctx.Items.ContainsKey "RequestId"
            then Log.ForContext("RequestId", ctx.Items.["RequestId"])
            else Log.Logger

    /// The context combinator allows access to data and services within the current HttpContext from which you can create a new HttpContext.
    let context (contextMap: HttpContext -> HttpHandler) : HttpHandler =
      fun (next : (HttpContext -> Task<HttpContext option>)) (ctx : HttpContext)  ->
          let nextHandler : HttpHandler = contextMap ctx
          let result = nextHandler next
          result ctx


type SerilogAdapter() =
    /// Wraps a HttpHandler with logging enabled using the given configuration.
    static member Enable(app: HttpHandler, config: SerilogConfig) : HttpHandler =

        fun (next: HttpFunc) (context: HttpContext) ->
            task {
                let requestId =
                    if context.Items.ContainsKey "RequestId"
                    then
                      // use existing request id
                      string (context.Items.["RequestId"])
                    else
                      // generate it and set it in the context
                      let id = Guid.NewGuid().ToString("N")
                      context.Items.Add("RequestId", id)
                      id

                let stopwatch = Stopwatch.StartNew()
                let requestLogger = Log.ForContext(RequestLogEnricher(context, config, requestId))
                requestLogger.Information(config.RequestMessageTemplate)

                try
                    let appliedHandler = app next
                    let! result = appliedHandler context
                    match result with
                    | Some resultContext ->
                        let responseLogger = Log.ForContext(ResponseLogEnricher(resultContext, config, stopwatch, requestId))
                        responseLogger.Information(config.ResponseMessageTemplate)
                        return Some resultContext
                    | None ->
                        let passThroughLoggerContext = Log.ForContext(PassThroughLogEnricher(context, stopwatch))
                        passThroughLoggerContext.Information("Passing through logger HttpHandler")
                        return None
                with
                | ex ->
                    let errorLogger = Log.ForContext(ErrorLogEnricher(context, stopwatch, requestId))
                    errorLogger.Error(ex, config.ErrorMessageTemplate)
                    let errorHandlerResult = config.ErrorHandler ex context
                    let nextFromError = errorHandlerResult next
                    return! nextFromError context
            }


    /// Wraps a HttpHandler with logging enables using the default configuration.
    static member Enable(app: HttpHandler) = SerilogAdapter.Enable(app, SerilogConfig.defaults)
