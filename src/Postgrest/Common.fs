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
  
    type PostgrestBuilder = {
        Query: Query
        Body:  RequestBody
    }
    
    type Columns =
        | All
        | Cols of string list
        
    type Column = string
    
    
    // let withAuth (token: string) (connection: PostgrestConnection): PostgrestConnection =
    //     let bearer = $"Bearer {token}"
    //     match connection.Headers.ContainsKey "Authorization" with
    //     | true  -> connection.Headers.Item "Authorization" <- bearer
    //     | false -> connection.Headers.Add("Authorization", bearer)
    //     
    //     connection    
    
    let internal addRequestHeaders (headers: Map<string, string>) (httpRequestHeaders: HttpRequestHeaders): unit =
        headers |> Seq.iter (fun (KeyValue(k, v)) -> httpRequestHeaders.Add(k, v))
    
    // let private addRequestHeader (key: string) (value: string) (client: HttpClient): unit =
    //     client.DefaultRequestHeaders.Add(key, value)
    //
    // let internal addRequestHeaders (headers: Dictionary<string, string>) (client: HttpClient): unit =
    //     headers |> Seq.iter (fun (KeyValue(k, v)) -> client |> addRequestHeader k v)
    
    let internal joinQueryParams (queryParams: string list): string =
        queryParams |> List.reduce(fun acc item -> $"{acc},{item}")
    
    let internal parseColumns (columns: Columns): string =
        match columns with
        | Cols cols ->
            match cols.IsEmpty with
            | true -> "*"
            | _    -> cols |> joinQueryParams
        | _         -> "*"
        
    let internal parseOptionalQueryString (queryString: string option): string =
        match queryString with
        | Some value -> value
        | None       -> ""