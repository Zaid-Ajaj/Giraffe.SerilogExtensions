# Giraffe.SerilogExtensions [![Build Status](https://travis-ci.org/Zaid-Ajaj/Giraffe.SerilogExtensions.svg?branch=master)](https://travis-ci.org/Zaid-Ajaj/Giraffe.SerilogExtensions) [![Nuget](https://img.shields.io/nuget/v/Giraffe.SerilogExtensions.svg?colorB=green)](https://www.nuget.org/packages/Giraffe.SerilogExtensions)

[Giraffe](https://github.com/giraffe-fsharp/Giraffe) plugin to use the awesome [Serilog](https://github.com/serilog/serilog) library as the logger for your application

### Install
Install from Nuget:
```bash
# using nuget client
dotnet add package Giraffe.SerilogExtensions
# using Paket
.paket/paket.exe add Giraffe.SerilogExtensions --project path/to/Your.fsproj
```

### Usage
Wrap an existing `HttpHandler` with `SerilogAdapter.Enable`:
```fs
open Giraffe
open Giraffe.SerilogExtensions
open Serilog 

// your application
let webApp = GET >=> route "/" >=> text "Home"

// Enable logging on an exisiting HttpHandler 
let webAppWithLogging = SerilogAdapter.Enable(webApp)

// Configure serilog 
Log.Logger <- 
  LoggerConfiguration()
    .Destructure.FSharpTypes()
    .WriteTo.Console() // from Serilog.Sinks.Console
    .CreateLogger() 

(* configure Giraffe to run here... *)
```
Now `dotnet run` and `curl http://localhost:8080` to get the following logs:
```fs
[20:35:42 INF] GET Request at /
[20:35:42 INF] GET Response (StatusCode 200) at / took 121 ms
```
These request and response log events contain many properties that are extracted from the `HttpContext`, enable a detailed console sink with `JsonFormatter` to see what properties are extracted from the http context:
```fs
open Serilog.Formatting.Json
(*
  ...
*)
Log.Logger <- 
  LoggerConfiguration()
    .Destructure.FSharpTypes()
    .WriteTo.Console() // from Serilog.Sinks.Console
    .WriteTo.Console(JsonFormatter())
    .CreateLogger() 
``` 
Now there logs become as follows, since there are two sinks, one is normal console log and the other is detailed LogEvent in JSON format, you can tell from the headers that I am using Postman for testing.
```fs
[19:43:12 INF] GET Request at /index
{"Timestamp":"2018-12-22T19:43:12.5837113+01:00","Level":"Information","MessageTemplate":"{Method} Request at {Path}","Properties":{"RequestId":"2b47246b-ba4f-4b24-9d12-fe1827fcfa87","Type":"Request","Path":"/index","Method":"GET","Host":"localhost","Port":5000,"Query":{},"RequestHeaders":{"Accept":"*/*","Accept-Encoding":"gzip, deflate","Cache-Control":"no-cache","Connection":"keep-alive","Host":"localhost:5000","Postman-Token":"61f5470e-27ad-4a98-b074-c7e41bceb1f7","User-Agent":"PostmanRuntime/7.4.0"},"UserAgent":"PostmanRuntime/7.4.0","Body":"","ContentType":""}}

[19:43:12 INF] GET Response (StatusCode 200) at /index took 163 ms
{"Timestamp":"2018-12-22T19:43:12.7419652+01:00","Level":"Information","MessageTemplate":"{Method} Response (StatusCode {StatusCode}) at {Path} took {Duration} ms","Properties":{"Duration":163,"Path":"/index","RequestId":"2b47246b-ba4f-4b24-9d12-fe1827fcfa87","Type":"Response","Method":"GET","StatusCode":200,"ContentType":"text/plain; charset=utf-8"}}
```
Logs from the same roundtrip will include a `RequestId` property that is the same for these logs to trace them back using your favorite log server. 

### Use the logger from inside the WebPart
You can get a reference for a logger with the `RequestId` attached to it from inside a `HttpHandler`:
```fs
let webApp = 
  choose [ GET >=> route "/" >=> text "Home"
           GET >=> route "/index" 
               >=> context (fun ctx ->
                     // get the contextual logger
                     let logger = ctx.Logger() 
                     logger.Information("Read my {RequestId}")
                     text "Some response") ]
```
the `Logger()` method is an extension method to `HttpContext`. 

### Ignore log fields
As you can see, there many fields being logged from the request and response. You can configure the logger to ignore some fields:
```fs
let serilogConfig = 
  { SerilogConfig.defaults with
      IgnoredRequestFields = 
        Ignore.fromRequest
        |> Field.host
        |> Field.queryString
      IgnoredResponseFields = 
        Ignore.fromResponse
        |> Field.contentType }

let webAppWithLogging = SerilogAdapter.Enable(webApp, serilogConfig)
```
### Error Handling
Error handling within the Serilog `HttpHandler` is also handled by Serilog and not Giraffes's internal logger. The error handler is of type: `Exception -> HttpContext -> HttpHandler` with the default handler returning a generic error message from the server:
```fs
let errorHandler ex httpContext = 
    text "Internal Server Error" >=> setStatusCode 500
    
```
You can override this error handler from the config:
```fs
let serilogConfig = 
 { SerilogConfig.defaults with 
    ErrorHandler = 
      fun ex httpContext -> 
        // NancyFx-style apologetic message :D
        text "Sorry, something went terribly wrong!"
        >=> setStatusCode 500 }

let webAppWithLogging = SerilogAdapter.Enable(webApp, serilogConfig)
```

## Builds

![Build History](https://buildstats.info/travisci/chart/Zaid-Ajaj/Giraffe.SerilogExtensions)


### Building


Make sure the following **requirements** are installed in your system:

* [dotnet SDK](https://www.microsoft.com/net/download/core) 2.0 or higher
* [Mono](http://www.mono-project.com/) if you're on Linux or macOS.

```
> build.cmd // on windows
$ ./build.sh  // on unix
```

### Watch Tests

The `WatchTests` target will use [dotnet-watch](https://github.com/aspnet/Docs/blob/master/aspnetcore/tutorials/dotnet-watch.md) to watch for changes in your lib or tests and re-run your tests on all `TargetFrameworks`

```
./build.sh WatchTests
```