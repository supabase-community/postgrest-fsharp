namespace Postgrest

open System.Net.Http.Headers
open Postgrest.Connection

/// Contains helper functions for another modules and shared types
[<AutoOpen>]
module Common =
    /// Represents query with neccesary info
    type Query = {
        Connection:  PostgrestConnection
        Table:       string
        QueryString: string
    }
    
    /// Represents queries on which filtering could be performed
    type FilterRequestType =
        | Select
        | Delete
        | Update
        
    /// Represents request body
    type RequestBody = string
    
    /// Represents columns that would be selected by select query
    type Columns =
        | All
        | Columns of string list
        
    /// Represents column
    type Column = string
    
    /// Represents type on which filtering operations could be performed
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
    
    /// Adds HttpRequestHeaders to given headers Map
    let internal addRequestHeaders (headers: Map<string, string>) (httpRequestHeaders: HttpRequestHeaders): unit =
        headers |> Seq.iter (fun (KeyValue(k, v)) -> httpRequestHeaders.Add(k, v))
    
    /// Joins list of query params to valid string representation
    let internal joinQueryParams (queryParams: string list): string =
        queryParams |> List.reduce(fun acc item -> $"{acc},{item}")
    
    /// Converts `Columns` to it's string representation
    let parseColumns (columns: Columns): string =
        match columns with
        | Columns cols ->
            match cols.IsEmpty with
            | true -> "*"
            | _    -> cols |> joinQueryParams
        | _         -> "*"
        
    /// Parses value of optional query string. If not given empty string is returned
    let internal parseOptionalQueryString (queryString: string option): string = ("", queryString) ||> Option.defaultValue