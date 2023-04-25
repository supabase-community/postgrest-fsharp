namespace Postgrest

open System.Net.Http

/// Contains CE for creating connection
[<AutoOpen>]
module Connection =
    /// Represents base connection
    type PostgrestConnection = {
        Url: string
        Headers: Map<string, string>
        HttpClient: HttpClient
    }
    
    type PostgrestConnectionBuilder() =
        member _.Yield _ =
            {   Url = ""
                Headers =  Map []
                HttpClient = new HttpClient() }
       
        [<CustomOperation("url")>]
        member _.Url(connection, url) =
            { connection with Url = url }
        
        [<CustomOperation("headers")>]
        member _.Headers(connection, headers) =
            { connection with Headers = headers }
            
        [<CustomOperation("httpClient")>]
        member _.HttpClient(connection, httpClient) =
            { connection with HttpClient = httpClient }
            
    let postgrestConnection = PostgrestConnectionBuilder()