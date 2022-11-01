namespace Postgrest.Client

open System.Text
open FSharp.Json
open Postgrest.Connection
open System.Net.Http
    
[<AutoOpen>]
module Client =
    type Query = {
        Connection:  PostgrestConnection
        Table:       string
        QueryString: string
    }
    
    type GetRequest = {
        Query:             Query
        QueryFilterString: string option
    }
    
    type PostRequest = {
        Query: Query
        Body:  string
    }
    
    type FilterValue =
        | String of string
        | Int    of int
        | Double of double
        | Float  of float
        | Bool   of bool
    
    type Filter =
        | EQ  of  string * FilterValue
        | GT  of  string * FilterValue
        | GTE of  string * FilterValue
        | LT  of  string * FilterValue
        | LTE of  string * FilterValue
        | NEQ of  string * FilterValue
        | NOT of  Filter
        | OR  of  Filter * Filter
        | AND of  Filter * Filter
        
    type FilterIn<'a> = string * 'a list
        
    let private addHeader (key: string) (value: string) (client: HttpClient) = 
        client.DefaultRequestHeaders.Add(key, value)
    
    let private addHeaders (headers: (string * string) list) (client: HttpClient) =
        headers
        |> List.iter (fun (key, value) -> client |> addHeader key value)
    
    let private parseColumns (columns: string list option): string =
        match columns with
        | Some cols ->
            match cols.IsEmpty with
            | true -> "*"
            | _    -> cols |> List.reduce(fun acc item -> $"{acc},{item}")
        | None   -> "*"
        
    let parseFilterValue (filterValue: FilterValue): string =
        match filterValue with
        | String s -> s
        | Int    i -> i.ToString()
        | Double d -> d.ToString()
        | Float  f -> f.ToString()
        | Bool   b -> b.ToString()
    
    let rec private buildFilterString (filter: Filter): string = 
        match filter with
        | EQ  (field, value) -> $"{field}=eq." + parseFilterValue value
        | GT  (field, value) -> $"{field}=gt." + parseFilterValue value
        | GTE (field, value) -> $"{field}=gte." + parseFilterValue value
        | LT  (field, value) -> $"{field}=lt." + parseFilterValue value
        | LTE (field, value) -> $"{field}=lte." + parseFilterValue value
        | NEQ (field, value) -> $"{field}=neq." + parseFilterValue value
        | NOT f              -> "not." + buildFilterString f
        | OR  (f1, f2)       -> "or=(" + buildFilterString f1 + "," + buildFilterString f2 + ")"
        | AND (f1, f2)       -> "and=(" + buildFilterString f1 + "," + buildFilterString f2 + ")"
    
    let from (table: string) (conn: PostgrestConnection): Query =
        { Connection  = conn
          Table       = table
          QueryString = "" }
        
    let select (columns: string list option) (query: Query): GetRequest =
        let queryString = parseColumns columns
        
        { Query             = { query with QueryString = $"?select={queryString}" }
          QueryFilterString = None }
        
    let insert (data: 'a) (query: Query): PostRequest =
        let body = Json.serialize data
        
        { Query = { query with QueryString = "?insert" }
          Body  = body }
        
    let private concatQueryFilterString (queryFilterString: string option): string =
        match queryFilterString with
            | Some fs -> fs
            | _       -> ""
    
    let filter (filter: Filter) (request: GetRequest): GetRequest =
        let currentQueryFilterString = request.QueryFilterString |> concatQueryFilterString
        let filterString = $"{currentQueryFilterString}&" + (filter |> buildFilterString)
        
        { Query             = request.Query
          QueryFilterString = Some filterString }
        
    let in_ (filterIn: string * 'a list) (request: GetRequest): GetRequest =
        let stringValues = (snd filterIn) |> List.map (fun item -> item.ToString())
        let currentQueryFilterString = request.QueryFilterString |> concatQueryFilterString
        
        let filterString = $"{currentQueryFilterString}&{fst filterIn}=in." + "(" +
                           (stringValues |> List.reduce(fun acc item -> $"{acc},{item}")) + ")"
        { Query             = request.Query
          QueryFilterString = Some filterString }
        
    let executeSelect<'T> (request: GetRequest) =
        let result =
            task {
                use client = new HttpClient()
                
                let headers = request.Query.Connection.Headers
                client |> addHeaders (Map.toList headers)
                
                let! response =
                    let query = request.Query
                    
                    let queryFilterString =
                        match request.QueryFilterString with
                        | Some qfs -> qfs
                        | _        -> ""
                    let url = $"{query.Connection.Url}/{query.Table}{query.QueryString}{queryFilterString}"
                    
                    client.GetAsync(url)
                return! response.Content.ReadAsStringAsync()
            } |> Async.AwaitTask |> Async.RunSynchronously
        Json.deserialize<'T> result
        
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
        
      
    // let execute (request: Request) =
    //     let result =
    //         task {
    //             use client = new HttpClient()
    //             
    //             let headers =
    //                 match request with
    //                     | GET  r -> r.Query.Connection.Headers
    //                     | POST r -> r.Query.Connection.Headers
    //             client |> addHeaders (Map.toList headers)
    //             
    //             let! response =
    //                 match request with
    //                 | GET  r ->
    //                     let query = r.Query
    //                     
    //                     let queryFilterString =
    //                         match r.QueryFilterString with
    //                         | Some qfs -> qfs
    //                         | _        -> ""
    //                     let url = $"{query.Connection.Url}/{query.Table}{query.QueryString}{queryFilterString}"
    //                     
    //                     client.GetAsync(url)
    //                 | POST r ->
    //                     let query = r.Query
    //                     let url = $"{query.Connection.Url}/{query.Table}{query.QueryString}"
    //                     let content = new StringContent(r.Body)
    //                     
    //                     client.PostAsync(url, content)
    //             return! response.Content.ReadAsStringAsync()
    //         } |> Async.AwaitTask |> Async.RunSynchronously
    //     printfn $"RESULT: {result}"
    //     ()

// namespace Postgrest.Client
//
// open FSharp.Json
// open Postgrest.Connection
// open System.Net.Http
//     
// [<AutoOpen>]
// module Client =
//     type Query = {
//         Connection:        PostgrestConnection
//         Table:             string
//         QueryString:       string
//     }
//     
//     type GetRequest = {
//         Query: Query
//         QueryFilterString: string option
//     }
//     
//     type PostRequest = {
//         Query: Query
//         Body:  string
//     }
//     
//     type Request =
//         | GET  of GetRequest
//         | POST of PostRequest
//         
//     type Filter =
//         | EQ of string * string
//         
//     let private addHeader (key: string) (value: string) (client: HttpClient) = 
//         client.DefaultRequestHeaders.Add(key, value)
//     
//     let private addHeaders (headers: (string * string) list) (client: HttpClient) =
//         headers
//         |> List.iter (fun (key, value) -> client |> addHeader key value)
//     
//     let private parseColumns (columns: string list option): string =
//         match columns with
//         | Some cols ->
//             match cols.IsEmpty with
//             | true -> "*"
//             | _    -> cols |> List.reduce(fun acc item -> $"{acc},{item}")
//         | None   -> "*"
//     
//     let private buildFilterString (filter: Filter): string =
//         match filter with
//         | EQ (field, value) -> $"${field}=eq.{value}"
//     
//     let from (table: string) (conn: PostgrestConnection): Query =
//         { Connection = conn
//           Table = table
//           QueryString = "" }
//         
//     let select (columns: string list option) (query: Query): GetRequest =
//         let queryString = parseColumns columns
//         
//         { Query = { query with QueryString = $"?select={queryString}" }
//           QueryFilterString = None }
//         
//     let insert data (query: Query): PostRequest =
//         let body = Json.serialize data
//         
//         { Query = { query with QueryString = "?insert}" }
//           Body  = body }
//         
//     let filter (filter: Filter) (request: GetRequest): GetRequest =
//         let filterString = filter |> buildFilterString
//         
//         { request with QueryFilterString = Some filterString }
//       
//     let execute (request: Request) =
//         let result =
//             task {
//                 use client = new HttpClient()
//                 
//                 let headers =
//                     match request with
//                         | GET  r -> r.Query.Connection.Headers
//                         | POST r -> r.Query.Connection.Headers
//                 client |> addHeaders (Map.toList headers)
//                 
//                 let! response =
//                     match request with
//                     | GET  r ->
//                         let query = r.Query
//                         
//                         let queryFilterString =
//                             match r.QueryFilterString with
//                             | Some qfs -> qfs
//                             | _        -> ""
//                         let url = $"{query.Connection.Url}/{query.Table}{query.QueryString}{queryFilterString}"
//                         
//                         client.GetAsync(url)
//                     | POST r ->
//                         let query = r.Query
//                         let url = $"{query.Connection.Url}/{query.Table}{query.QueryString}"
//                         let content = new StringContent(r.Body)
//                         
//                         client.PostAsync(url, content)
//                 return! response.Content.ReadAsStringAsync()
//             } |> Async.AwaitTask |> Async.RunSynchronously
//         printfn $"RESULT: {result}"
//         ()