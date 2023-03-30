namespace Postgrest

open System
open System.Text
open System.Net.Http
open FSharp.Json
open Postgrest.Connection
open Postgrest.Common
open Postgrest.Http
    
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
    
    let private executeSelect<'T> (pfb: PostgrestFilterBuilder): Result<HttpResponseMessage, PostgrestError> =
        let urlSuffix = pfb |> getUrlSuffixFromPostgresFilterBuilder
        
        pfb.Query.Connection |> get urlSuffix None
        
    let private executeDelete (pfb: PostgrestFilterBuilder): Result<HttpResponseMessage, PostgrestError> =
        let urlSuffix = pfb |> getUrlSuffixFromPostgresFilterBuilder
            
        pfb.Query.Connection |> Http.delete urlSuffix (Some (Map [ "Prefer" , "return=representation" ] )) None
    
    let private executeUpdate (pfb: PostgrestFilterBuilder): Result<HttpResponseMessage, PostgrestError> =
        let urlSuffix = pfb |> getUrlSuffixFromPostgresFilterBuilder
                
        let contentBody =
            match pfb.Body with
            | Some body -> body
            | None      -> raise (Exception "Missing body")    
        let content = new StringContent(contentBody, Encoding.UTF8, "application/json")
        
        pfb.Query.Connection |> patch urlSuffix (Some (Map [ "Prefer" , "return=representation" ] )) content
        
    let getResponseBody (responseMessage: HttpResponseMessage): string = 
        responseMessage.Content.ReadAsStringAsync()
        |> Async.AwaitTask
        |> Async.RunSynchronously
     
    let execute<'T> (pfb: PostgrestFilterBuilder): Result<'T, PostgrestError> = 
        let response =
            match pfb.RequestType with
            | Select -> pfb |> executeSelect
            | Delete -> pfb |> executeDelete
            | Update -> pfb |> executeUpdate
            
        deserializeResponse<'T> response
   
    let executeInsert (pb: PostgrestBuilder) =
        let query = pb.Query
        let urlSuffix = $"{query.Table}{query.QueryString}"
        
        pb.Query.Connection |> get urlSuffix None
        
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