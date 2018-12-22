namespace Giraffe.SerilogExtensions

open System
open System.Diagnostics
open Giraffe
open Serilog
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2
open System.Threading.Tasks

[<AutoOpen>]
module Extensions = 
    type HttpContext with 
        /// Returns a logger with the RequestId attached to it, if present.
        member ctx.Logger() : ILogger =
            if ctx.Items.ContainsKey "RequestId"
            then Log.ForContext("RequestId", ctx.Items.["RequestId"])
            else Log.Logger

    let context (contextMap: HttpContext -> HttpHandler) : HttpHandler =  
      fun (next : (HttpContext -> Task<HttpContext option>)) (ctx : HttpContext)  -> 
          let nextHandler : HttpHandler = contextMap ctx
          let result = nextHandler next
          result ctx


type SerilogAdapter() = 
    /// Wraps a HttpHandler with logging enabled using the given given configuration
    static member Enable(app: HttpHandler, config: SerilogConfig) : HttpHandler = 
        
        fun (next: HttpFunc) (context: HttpContext) ->
            task {
                let requestId = Guid.NewGuid().ToString()
                let stopwatch = Stopwatch.StartNew()
                let requestLogger = Log.ForContext(RequestLogEnricher(context, config, requestId))

                requestLogger.Information(config.RequestMessageTemplate)
                try
                    context.Items.Add("RequestId", requestId)
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
            

            
            

        

    /// Wraps a HttpHandler with logging enables using the default configuration
    static member Enable(app: HttpHandler) = SerilogAdapter.Enable(app, SerilogConfig.defaults)