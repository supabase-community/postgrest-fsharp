namespace Postgrest

open System.Net.Http.Headers
open Postgrest.Connection

[<AutoOpen>]
module Common =
    type Query = {
        Connection:  PostgrestConnection
        Table:       string
        QueryString: string
    }
    
    type FilterRequestType =
        | Select
        | Delete
        | Update
        
    type RequestBody = string
    
    type Columns =
        | All
        | Columns of string list
        
    type Column = string
    
    type PostgrestFilterBuilder = {
        Query            : Query
        QueryFilterString: string option
        QueryInString    : string option
        QueryIsString    : string option
        QueryOrderString : string option
        QueryLimitString : string option
        QueryOffsetString: string option
        QueryLikeString  : string option
        QueryILikeString : string option
        QueryFtsString   : string option
        Body             : RequestBody option
        RequestType      : FilterRequestType   
    }
    
    let internal addRequestHeaders (headers: Map<string, string>) (httpRequestHeaders: HttpRequestHeaders): unit =
        headers |> Seq.iter (fun (KeyValue(k, v)) -> httpRequestHeaders.Add(k, v))
    
    let inline joinQueryParams (queryParams: string list): string =
        queryParams |> List.reduce(fun acc item -> $"{acc},{item}")
    
    let inline parseColumns (columns: Columns): string =
        match columns with
        | Columns cols ->
            match cols.IsEmpty with
            | true -> "*"
            | _    -> cols |> joinQueryParams
        | _         -> "*"
        
    let inline parseOptionalQueryString (queryString: string option): string = ("", queryString) ||> Option.defaultValue