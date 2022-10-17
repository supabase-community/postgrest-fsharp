module Postgrest.StatelessClient

module StatelessClient =
    open Postgrest.Connection
        
    let connect (url: string) (headers: Option<Map<string, string>>): Connection.PostgrestClientConnection = {
        Url = url
        Headers = headers
    }
        
    let execute (connection: Connection.PostgrestClientConnection) =
        printfn $"Call on URL= {connection.Url} with apiKey= {connection.Headers}"