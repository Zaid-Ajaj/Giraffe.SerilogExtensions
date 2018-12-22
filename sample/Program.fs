open System
open Giraffe
open FSharp.Control.Tasks.V2
open Giraffe.SerilogExtensions
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Server.Kestrel
open Serilog
open Serilog.Formatting.Json
open System.Threading.Tasks
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection

type Maybe<'a> = 
  | Nothing
  | Just of 'a

type Rec = { First: string; Job: Maybe<string> }

let app : HttpHandler = 
  choose [ GET >=> route "/index" >=> text "Index"
           POST >=> route "/echo" >=> text "Echo" 
           GET >=> route "/destructure" 
               >=> context (fun ctx ->
                    let logger = ctx.Logger()
                    let genericUnion =  Maybe.Just { First = "Zaid"; Job = Nothing }
                    logger.Information("Generic Union with Record {@Union}", genericUnion)

                    let result = Ok (Just "for now!")
                    logger.Information("Result {@Value}", result)

                    let simpleList = [1;2;3;4;5]
                    logger.Information("Simple list {@List}", simpleList)

                    let complexList = [ box (Just "this?"); box ({ First = "Zaid"; Job = Nothing }) ]
                    logger.Information("Complex list {@List}", complexList) 
                    
                    text "Done")

           GET >=> route "/internal" 
               >=> context (fun ctx ->
                     let logger = ctx.Logger()
                     logger.Information("Using internal logger")
                     text "Internal") 

           GET >=> route "/fail" >=> context (fun ctx -> failwith "Fail miserably") ]
 
let simpleApp : HttpHandler = 
  choose [ GET >=> route "/" >=> text "Home" 
           GET >=> route "/other" >=> text "Other route" ]   

let appWithLogger = SerilogAdapter.Enable(app)

Log.Logger <- 
  LoggerConfiguration()
    .Destructure.FSharpTypes()
    .WriteTo.Console()
    .WriteTo.Console(new JsonFormatter())
    .CreateLogger()

type Startup() =
    member __.ConfigureServices (services : IServiceCollection) =
        // Register default Giraffe dependencies
        services.AddGiraffe() |> ignore

    member __.Configure (app : IApplicationBuilder)
                        (env : IHostingEnvironment)
                        (loggerFactory : ILoggerFactory) =
        // Add Giraffe to the ASP.NET Core pipeline
        app.UseGiraffe appWithLogger

[<EntryPoint>]
let main _ =
    WebHostBuilder()
        .UseKestrel()
        .UseStartup<Startup>()
        .Build()
        .Run()
    0