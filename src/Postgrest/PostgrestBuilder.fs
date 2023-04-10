namespace Postgrest

open Postgrest.Http

[<AutoOpen>]
module PostgrestBuilder =
    type PostgrestBuilder = {
        Query: Query
        Body:  RequestBody
    }
     
    let execute (pb: PostgrestBuilder) =
        let query = pb.Query
        let urlSuffix = $"{query.Table}{query.QueryString}"
        
        pb.Query.Connection |> get urlSuffix None