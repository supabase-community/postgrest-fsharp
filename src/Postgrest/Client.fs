namespace Postgrest

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
          QueryInString     = None
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
          QueryInString     = None
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
          QueryInString     = None
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
        
    let getResponseBody (responseMessage: HttpResponseMessage): string = 
        responseMessage.Content.ReadAsStringAsync()
        |> Async.AwaitTask
        |> Async.RunSynchronously
     
    let updateBearer (bearer: string) (connection: PostgrestConnection): PostgrestConnection =
        let formattedBearer = $"Bearer {bearer}"
        let headers =
            match connection.Headers.ContainsKey "Authorization" with
            | true  ->
                connection.Headers
                |> Seq.map (fun (KeyValue (k, v)) ->
                        match k with
                        | "Authorization" -> (k, formattedBearer)
                        | _               -> (k, v))
                |> Map
            | false ->
                connection.Headers |> Map.add "Authorization" formattedBearer
        { connection with Headers = headers }