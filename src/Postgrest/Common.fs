namespace Postgrest

open System.Net.Http
open Postgrest.Connection

[<AutoOpen>]
module Common =
    type Query = {
        Connection:  PostgrestConnection
        Table:       string
        QueryString: string
    }
    
    type RequestBody = string
    
    type FilterRequestType =
        | Select
        | Delete
        | Update
    
    type PostgrestFilterBuilder = {
        Query            : Query
        QueryFilterString: string option
        QueryOrderString : string option
        QueryLimitString : string option
        QueryOffsetString: string option
        Body             : RequestBody option
        RequestType      : FilterRequestType   
    }
  
    type PostgrestBuilder = {
        Query: Query
        Body:  RequestBody
    }
    
    type Columns =
        | ALL
        | COLS of string list
    
    let private addHeader (key: string) (value: string) (client: HttpClient) =
        client.DefaultRequestHeaders.Add(key, value)
    
    let internal addHeaders (headers: (string * string) list) (client: HttpClient) =
        headers
        |> List.iter (fun (key, value) -> client |> addHeader key value)
    
    let internal parseColumns (columns: Columns): string =
        match columns with
        | COLS cols ->
            match cols.IsEmpty with
            | true -> "*"
            | _    -> cols |> List.reduce(fun acc item -> $"{acc},{item}")
        | ALL      -> "*"
        
    let internal parseOptionalQueryString (queryString: string option): string =
        match queryString with
        | Some value -> value
        | None       -> ""