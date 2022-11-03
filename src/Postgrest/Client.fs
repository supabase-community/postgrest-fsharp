namespace Postgrest.Client

open FSharp.Json
open Postgrest.Connection
open Postgrest.Common
open System.Text
open System.Net.Http
    
[<AutoOpen>]
module Client =
    type Columns =
        | ALL
        | COLS of string list
        
    let private addHeader (key: string) (value: string) (client: HttpClient) = 
        client.DefaultRequestHeaders.Add(key, value)
    
    let private addHeaders (headers: (string * string) list) (client: HttpClient) =
        headers
        |> List.iter (fun (key, value) -> client |> addHeader key value)
    
    let private parseColumns (columns: Columns): string =
        match columns with
        | COLS cols ->
            match cols.IsEmpty with
            | true -> "*"
            | _    -> cols |> List.reduce(fun acc item -> $"{acc},{item}")
        | ALL      -> "*"
    
    let from (table: string) (conn: PostgrestConnection): Query =
        { Connection  = conn
          Table       = table
          QueryString = "" }
        
    let select (columns: Columns) (query: Query): GetRequest =
        let queryString = parseColumns columns
        
        { Query             = { query with QueryString = $"?select={queryString}" }
          QueryFilterString = None
          QueryOrderString  = None
          QueryLimitString  = None
          QueryOffsetString = None }
        
    let insert (data: 'a) (query: Query): PostRequest =
        let body = Json.serialize data
        
        { Query = { query with QueryString = "?insert" }
          Body  = body }
        
    let private parseOptionalQueryString (queryString: string option): string =
        match queryString with
        | Some value -> value
        | None       -> ""
        
    let executeSelect<'T> (request: GetRequest) =
        let result =
            task {
                use client = new HttpClient()
                
                let headers = request.Query.Connection.Headers
                client |> addHeaders (Map.toList headers)
                
                let! response =
                    let query = request.Query
                    
                    let queryFilterString = request.QueryFilterString |> parseOptionalQueryString
                    let queryOrderString = request.QueryOrderString |> parseOptionalQueryString
                    let queryLimitString = request.QueryLimitString |> parseOptionalQueryString
                    let queryOffsetString = request.QueryOffsetString |> parseOptionalQueryString
                        
                    let url =
                        query.Connection.Url + $"/{query.Table}" + query.QueryString +
                        queryFilterString + queryOrderString + queryLimitString +
                        queryOffsetString
                    
                    printfn $"{url}"
                    
                    client.GetAsync(url)
                return! response.Content.ReadAsStringAsync()
            } |> Async.AwaitTask |> Async.RunSynchronously
        try
            let res = Json.deserialize<'T> result
            printfn $"{res}"
        with
            _ -> printfn "misstype json"
        
        printfn $"{result}"
    let executeInsert (request: PostRequest) =
        let result =
            task {
                use client = new HttpClient()
                
                let headers = request.Query.Connection.Headers
                client |> addHeaders (Map.toList headers)
                // client |> addHeader "Content-Type" "application/json"
                
                let! response =
                    let query = request.Query
                    let url = $"{query.Connection.Url}/{query.Table}{query.QueryString}"
                    // let content = new StringContent(request.Body)
                    let content = new StringContent(request.Body, Encoding.UTF8, "application/json");
                    
                    printfn $"{url}"
                    printfn $"{content}"
                    
                    client.PostAsync(url, content)
                return! response.Content.ReadAsStringAsync()
            } |> Async.AwaitTask |> Async.RunSynchronously
        printfn $"RESULT: {result}"
        ()