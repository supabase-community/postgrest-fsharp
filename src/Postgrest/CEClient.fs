namespace Postgrest.CEClient

open System.Net.Http
open Postgrest.CEConnection 
    
[<AutoOpen>]
module CEClient =
    type Request = {
        Url: string
        Headers: Map<string, string>
        Body: Option<Map<string, string>>
    }
    
    let from (table: string) (conn: Conn): Request =
        let request = { Url = $"{conn.Url}{table}"
                        Headers = conn.Headers
                        Body = None }
        printfn $"table: {table}, conn: {request}"
        request
        
    let private addHeader (key: string) (value: string) (client: HttpClient) = 
        client.DefaultRequestHeaders.Add(key, value)
    
    let private addHeaders (headers: (string * string) list) (client: HttpClient) =
        headers
        |> List.iter (fun (key, value) -> client |> addHeader key value)
    
    let execute (request: Request) =
        let result =
            task {
                use client = new HttpClient()
                
                client |> addHeaders (Map.toList request.Headers)
                
                let! response = client.GetStringAsync(request.Url)
                return response
            } |> Async.AwaitTask |> Async.RunSynchronously
        printfn $"RESULT: {result}"
        ()