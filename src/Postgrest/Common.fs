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
    
    type FilterRequestType =
        | Select
        | Delete
        | Update
        
    type RequestBody = string
    
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
    
    let withAuth (token: string) (conn: PostgrestConnection): PostgrestConnection =
        let headers =
            match conn.Headers.ContainsKey "Authorization" with
            | true ->
                conn.Headers
                    .Remove("Authorization")
                    .Add("Authorization", $"Bearer {token}")
            | _    -> conn.Headers.Add("Authorization", $"Bearer {token}")
        { conn with Headers = headers}
    
    let private addRequestHeader (key: string) (value: string) (client: HttpClient) =
        client.DefaultRequestHeaders.Add(key, value)
    
    let internal addRequestHeaders (headers: (string * string) list) (client: HttpClient) =
        headers
        |> List.iter (fun (key, value) -> client |> addRequestHeader key value)
    
    let internal joinQueryParams (queryParams: string list): string =
        (queryParams |> List.reduce(fun acc item -> $"{acc},{item}"))
    
    let internal parseColumns (columns: Columns): string =
        match columns with
        | COLS cols ->
            match cols.IsEmpty with
            | true -> "*"
            | _    -> cols |> joinQueryParams
        | ALL      -> "*"
        
    let internal parseOptionalQueryString (queryString: string option): string =
        match queryString with
        | Some value -> value
        | None       -> ""