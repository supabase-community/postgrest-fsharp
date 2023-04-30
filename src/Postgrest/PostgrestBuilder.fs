namespace Postgrest

open Postgrest.Http
open Postgrest.Common

/// Contains functions and types for non filterable query (insert) 
[<AutoOpen>]
module PostgrestBuilder =
    /// Represents type on which no filtering operations could be performed 
    type PostgrestBuilder = {
        Query: Query
        Body:  RequestBody
    }
     
    /// Executes given `PostgrestBuilder`
    let execute<'T> (pb: PostgrestBuilder): Async<Result<'T, PostgrestError>> =
        let query = pb.Query
        let urlSuffix = $"{query.Table}{query.QueryString}"
        let content = getStringContent pb.Body
          
        async {
            let! response =
                post urlSuffix (Some (Map [ "Prefer" , "return=representation" ] ))content pb.Query.Connection
            return deserializeResponse<'T> response
        }
        
    /// Updates header to expect only one result to be returned
    let one (pb: PostgrestBuilder): PostgrestBuilder =
        let updatedQuery = one pb.Query
        { pb with Query = updatedQuery }