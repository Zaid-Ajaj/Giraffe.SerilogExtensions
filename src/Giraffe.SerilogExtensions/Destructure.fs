namespace Giraffe.SerilogExtensions

open Serilog.Core
open Serilog.Events
open System.Collections.Generic
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Fable.Remoting.Json 

module Json = 
    let private fableConverter = FableJsonConverter()
    let serialize (x: 'a) : string = 
        JsonConvert.SerializeObject(x, fableConverter)

    let rec convertToLogEventProperty (token : JToken) : LogEventPropertyValue = 
        match token.Type with
        | JTokenType.Null -> ScalarValue(null) :> LogEventPropertyValue
        | JTokenType.Array ->   
            token 
            |> unbox<JArray> 
            |> Seq.map convertToLogEventProperty 
            |> SequenceValue
            |> fun seq -> seq :> LogEventPropertyValue
        | JTokenType.Object ->
            token
            |> unbox<JObject>
            |> fun object -> object.Properties()
            |> Seq.map (fun prop -> 
                let key = ScalarValue prop.Name
                let value = convertToLogEventProperty prop.Value
                KeyValuePair(key, value))
            |> DictionaryValue
            |> fun dict -> dict :> LogEventPropertyValue
        | _ -> 
            ScalarValue(token.Value<obj>()) :> LogEventPropertyValue

        
type public FSharpTypesDestructurer() =
    interface Serilog.Core.IDestructuringPolicy with
        member __.TryDestructure(value,
                                 propertyValueFactory : ILogEventPropertyValueFactory,
                                 result: byref<LogEventPropertyValue>) =
            result <- (Json.serialize >> JToken.Parse >> Json.convertToLogEventProperty) value
            true

open Serilog.Configuration

[<AutoOpen>]
module public LoggerDestructuringConfigurationExtensions =
    type public LoggerDestructuringConfiguration with
        member public this.FSharpTypes() =
            this.With<FSharpTypesDestructurer>()
