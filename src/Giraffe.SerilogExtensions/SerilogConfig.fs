namespace Giraffe.SerilogExtensions

open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Net.Http.Headers

type FieldChoser<'t> = Choser of string list

type RequestLogData = RequestLogData
type ResponseLogData = ResponseLogData

/// Configuration to define what fields to ignore from either request or response and the error handler in case an exception is thrown
type SerilogConfig = 
    { IgnoredRequestFields : FieldChoser<RequestLogData>
      IgnoredResponseFields : FieldChoser<ResponseLogData>
      IgnoredRequestHeaders: string list
      RequestMessageTemplate : string
      ResponseMessageTemplate : string
      ErrorMessageTemplate : string
      ErrorHandler : System.Exception -> HttpContext -> HttpHandler }

    /// The default config with no ignored log event fields and a generic "Internal Server Error" 500 error handler.
    static member defaults = 
        { IgnoredRequestFields = Choser [ ] 
          IgnoredResponseFields = Choser [ ]
          IgnoredRequestHeaders = [ HeaderNames.Authorization; HeaderNames.Cookie ]
          RequestMessageTemplate = "{Method} Request at {Path}"
          ResponseMessageTemplate = "{Method} Response (StatusCode {StatusCode}) at {Path} took {Duration} ms"
          ErrorMessageTemplate = "Error at {Path} took {Duration} ms"
          ErrorHandler = fun ex httpContext -> setStatusCode 500 >=> text "Internal Server Error" }

module Ignore = 
    let fromRequest : FieldChoser<RequestLogData> = Choser [ ]
    let fromResponse : FieldChoser<ResponseLogData> = Choser [ ]

module Field = 
    let private ignoreReq (input: string) = 
        fun ((Choser ignored): FieldChoser<RequestLogData>) ->
            (Choser (("Request." + input) :: ignored)) : FieldChoser<RequestLogData>
    let private ignoreRes (input: string) = 
        fun ((Choser ignored): FieldChoser<ResponseLogData>) ->
            (Choser (("Response." + input) :: ignored)) : FieldChoser<ResponseLogData>

    let fullPath = ignoreReq "FullPath"
    let path = ignoreReq "Path"
    let host = ignoreReq "Host"
    let method = ignoreReq "Method"
    let requestHeaders = ignoreReq "Headers"
    let userAgent = ignoreReq "UserAgent"
    let contentType = ignoreReq "ContentType"
    let contentLength = ignoreReq "ContentLength"
    let queryString = ignoreReq "QueryString"
    let query = ignoreReq "Query"
    let responseContentLength = ignoreRes "ContentLength"
    let responseContentType = ignoreRes "ContentType"
