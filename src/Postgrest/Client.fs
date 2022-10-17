namespace Postgrest

module PostgrestClient =
    open Postgrest.Connection
    
    let mutable private connection: Connection.PostgrestClientConnection = {
        Url = ""
        Headers = None
    }
    
    let private _connect (url: string) (headers: Option<Map<string, string>>) =
        connection <- {
            Url = url
            Headers = headers
        }
       
    let connect (url: string) = None |> _connect url
        
    let connectWithHeaders (url: string) (headers: Option<Map<string, string>>) = headers |> _connect url
        
    let execute ()=
        printfn $"{connection}"
        // printfn $"Call on URL= {connection.Url} with apiKey= {connection.Headers}"