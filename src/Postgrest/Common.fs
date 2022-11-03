namespace Postgrest.Common

open Postgrest.Connection

[<AutoOpen>]
module Common =
    type Query = {
        Connection:  PostgrestConnection
        Table:       string
        QueryString: string
    }
    
    type GetRequest = {
        Query            : Query
        QueryFilterString: string option
        QueryOrderString : string option
        QueryLimitString : string option
        QueryOffsetString: string option
    }
    
    type PostRequest = {
        Query: Query
        Body:  string
    }
    
    type DeleteRequest = {
        Query: Query
        QueryFilterString: string option
    }
    
    type PostgrestFilterBuilder =
        | GetRequest of GetRequest
        | DeleteRequest of DeleteRequest