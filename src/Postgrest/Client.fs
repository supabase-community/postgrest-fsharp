namespace Postgrest

open System.Text
open System.Net.Http
open FSharp.Json
open Postgrest.Connection
open Postgrest.Common
    
/// Contains functions for base SQL queries. Communicates with [Postgrest](https://supabase.com/docs/guides/database/overview)
[<AutoOpen>]
module Client =
    /// Creates `Query` with connection and given table name
    let from (table: string) (conn: PostgrestConnection): Query =
        { Connection  = conn
          Table       = table
          QueryString = "" }
        
    /// Creates `PostgrestFilterBuilder`with select `Query` type
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
        
    /// Creates `PostgrestFilterBuilder`with delete `Query` type
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
        
    /// Creates `PostgrestFilterBuilder`with update `Query` type and given data as body
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
        
    /// Creates `PostgrestBuilder`with insert `Query` type and given data as body
    let insert<'T> (data: 'T) (query: Query): PostgrestBuilder =
        let body = Json.serialize data
        
        { Query = { query with QueryString = "?insert" }
          Body  = body }