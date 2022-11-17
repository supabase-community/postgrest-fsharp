namespace Postgrest

open System
open System.Text
open System.Net.Http
open FSharp.Json
open Postgrest.Connection
open Postgrest.Common
    
[<AutoOpen>]
module Client =   
    let from (table: string) (conn: PostgrestConnection): Query =
        { Connection  = conn
          Table       = table
          QueryString = "" }
        
    let select (columns: Columns) (query: Query): PostgrestFilterBuilder =
        let queryString = parseColumns columns
        { Query             = { query with QueryString = $"?select={queryString}" }
          QueryFilterString = None
          QueryIsString     = None
          QueryOrderString  = None
          QueryLimitString  = None
          QueryOffsetString = None
          QueryLikeString   = None
          QueryILikeString  = None
          QueryFtsString    = None
          Body              = None
          RequestType       = Select }
        
    let delete (query: Query): PostgrestFilterBuilder =
        { Query             = { query with QueryString = "?delete" }
          QueryFilterString = None
          QueryIsString     = None
          QueryOrderString  = None
          QueryLimitString  = None
          QueryOffsetString = None
          QueryLikeString   = None
          QueryILikeString  = None
          QueryFtsString    = None
          Body              = None
          RequestType       = Delete }
        
    let update (data: 'a) (query: Query): PostgrestFilterBuilder =
        let body = Json.serialize data
        
        { Query             = { query with QueryString = "?update" }
          QueryFilterString = None
          QueryIsString     = None
          QueryOrderString  = None
          QueryLimitString  = None
          QueryOffsetString = None
          QueryLikeString   = None
          QueryILikeString  = None
          QueryFtsString    = None
          Body              = Some body
          RequestType       = Update }
        
    let insert (data: 'a) (query: Query): PostgrestBuilder =
        let body = Json.serialize data
        
        { Query = { query with QueryString = "?insert" }
          Body  = body }
    
    let private executeSelect<'T> (pfb: PostgrestFilterBuilder): HttpResponseMessage =
        let result =
            task {
                let client = new HttpClient()
                
                let headers = pfb.Query.Connection.Headers
                client |> addRequestHeaders (Map.toList headers)
                
                let! response =
                    let query = pfb.Query
                    
                    let queryFilterString = pfb.QueryFilterString |> parseOptionalQueryString
                    let queryOrderString = pfb.QueryOrderString |> parseOptionalQueryString
                    let queryLimitString = pfb.QueryLimitString |> parseOptionalQueryString
                    let queryOffsetString = pfb.QueryOffsetString |> parseOptionalQueryString
                    let queryLikeString = pfb.QueryLikeString |> parseOptionalQueryString
                    let queryILikeString = pfb.QueryILikeString |> parseOptionalQueryString
                    let queryFtsString = pfb.QueryFtsString |> parseOptionalQueryString
                    let queryIsString = pfb.QueryIsString |> parseOptionalQueryString
                        
                    let url =
                        query.Connection.Url + "/" + query.Table + query.QueryString + queryFilterString
                        + queryOrderString + queryLimitString + queryOffsetString + queryLikeString
                        + queryILikeString + queryFtsString + queryIsString
                    
                    printfn $"{url}"
                    client.GetAsync(url)
                return response
            } |> Async.AwaitTask |> Async.RunSynchronously
        result
        
    let private executeDelete (pfb: PostgrestFilterBuilder): HttpResponseMessage =
        let result =
            task {
                let client = new HttpClient()
                
                let headers = pfb.Query.Connection.Headers
                client |> addRequestHeaders (Map.toList headers)
                
                let! response =
                    let query = pfb.Query
                    
                    let queryFilterString = pfb.QueryFilterString |> parseOptionalQueryString
                    let url = query.Connection.Url + "/" + query.Table + query.QueryString + queryFilterString
                    
                    printfn $"{url}"
                    client.DeleteAsync(url)
                return response
            } |> Async.AwaitTask |> Async.RunSynchronously
        result
    
    let private executeUpdate (pfb: PostgrestFilterBuilder): HttpResponseMessage =
        let result =
            task {
                let client = new HttpClient()
                
                let headers = pfb.Query.Connection.Headers
                client |> addRequestHeaders (Map.toList headers)
                
                let! response =
                    let query = pfb.Query
                    
                    let queryFilterString = pfb.QueryFilterString |> parseOptionalQueryString
                    let url = query.Connection.Url + "/" + query.Table + query.QueryString + queryFilterString
                    let contentBody =
                        match pfb.Body with
                        | Some body -> body
                        | None      -> raise (Exception "Missing body")
                        
                    let content = new StringContent(contentBody, Encoding.UTF8, "application/json")
                    
                    printfn $"{url}"
                    printf $"{contentBody}"
                    client.PatchAsync(url, content)
                return response
            } |> Async.AwaitTask |> Async.RunSynchronously
        result
        
    let execute (pfb: PostgrestFilterBuilder) =
        match pfb.RequestType with
        | Select -> pfb |> executeSelect
        | Delete -> pfb |> executeDelete
        | Update -> pfb |> executeUpdate
   
    let executeInsert (pb: PostgrestBuilder) =
        let result =
            task {
                let client = new HttpClient()
                
                let headers = pb.Query.Connection.Headers
                client |> addRequestHeaders (Map.toList headers)
                
                let! response =
                    let query = pb.Query
                    let url = $"{query.Connection.Url}/{query.Table}{query.QueryString}"
                    let content = new StringContent(pb.Body, Encoding.UTF8, "application/json");
                    
                    printfn $"{url}"
                    printfn $"{content}"
                    client.PostAsync(url, content)
                return response
            } |> Async.AwaitTask |> Async.RunSynchronously
        printfn $"RESULT: {result}"
        result
       
    let getResponseBody (responseMessage: HttpResponseMessage): string = 
        responseMessage.Content.ReadAsStringAsync()
        |> Async.AwaitTask
        |> Async.RunSynchronously
    
    let parseResponse<'T> (responseMessage: HttpResponseMessage): 'T =
        let response = responseMessage |> getResponseBody
        Json.deserialize<'T> response