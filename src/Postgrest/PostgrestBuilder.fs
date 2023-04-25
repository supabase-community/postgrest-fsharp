namespace Postgrest

open Postgrest.Http

/// Contains functions and types for non filterable query (insert) 
[<AutoOpen>]
module PostgrestBuilder =
    /// Represents type on which no filtering operations could be performed 
    type PostgrestBuilder = {
        Query: Query
        Body:  RequestBody
    }
     
    /// Executes given `PostgrestBuilder`
    let execute (pb: PostgrestBuilder) =
        let query = pb.Query
        let urlSuffix = $"{query.Table}{query.QueryString}"
        
        pb.Query.Connection |> get urlSuffix None