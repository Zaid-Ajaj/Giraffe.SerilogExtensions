namespace Giraffe.SerilogExtensions

open Serilog.Events
open System.Collections.Generic

module Enrichers = 
    let eventProperty name (value: obj) : LogEventProperty =
      match value with
      | :? string as text -> ScalarValue(text) :> LogEventPropertyValue
      | :? int as number -> ScalarValue(number) :> LogEventPropertyValue
      | :? Map<string, string> as dict ->
          Map.toList dict
          |> List.map (fun (key, value) -> 
              let key = ScalarValue(key)
              let value = ScalarValue(value)
              KeyValuePair<ScalarValue, LogEventPropertyValue>(key,value)) 
          |> fun pairs -> DictionaryValue(pairs) :> LogEventPropertyValue
      | :? int64 as longInt -> ScalarValue(longInt) :> LogEventPropertyValue
      | otherwise -> failwithf "Could not convert '%A' into a log event property value" otherwise
      |> fun eventPropValue -> LogEventProperty(name,  eventPropValue)
