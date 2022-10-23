namespace Postgrest

open System.Net
open System.Net.Http
open System.Net.Http.Headers

module PostgrestClient =
    open Postgrest.Connection
   
    let finalUrl = "https://uxdshctvypcjmjmwqndw.supabase.co/rest/v1/test?select=%2A&order=name.asc.nullslast"
    
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
        
    let connectWithHeaders (url: string) (headers: Map<string, string>) = Some(headers) |> _connect url
     
    let private addHeader (key: string) (value: string) (client: HttpClient) = 
        client.DefaultRequestHeaders.Add(key, value)
    
    let private addHeaders (headers: (string * string) list) (client: HttpClient) =
        headers
        |> List.iter (fun (key, value) -> client |> addHeader key value)
        
    let execute () =
        let result =
            task {
                use client = new HttpClient()
                
                client |> addHeaders [("Bearer", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InV4ZHNoY3R2eXBjam1qbXdxbmR3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE2NjU5MTQ4MDEsImV4cCI6MTk4MTQ5MDgwMX0.qUXjcOXhZJtYQX4q32YlCnppIpxbd8mf4x5-tA8tUpA")
                                      ("apiKey", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InV4ZHNoY3R2eXBjam1qbXdxbmR3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE2NjU5MTQ4MDEsImV4cCI6MTk4MTQ5MDgwMX0.qUXjcOXhZJtYQX4q32YlCnppIpxbd8mf4x5-tA8tUpA")]
                
                // client |> addHeader "Bearer" "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InV4ZHNoY3R2eXBjam1qbXdxbmR3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE2NjU5MTQ4MDEsImV4cCI6MTk4MTQ5MDgwMX0.qUXjcOXhZJtYQX4q32YlCnppIpxbd8mf4x5-tA8tUpA"
                // client |> addHeader "apiKey" "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InV4ZHNoY3R2eXBjam1qbXdxbmR3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE2NjU5MTQ4MDEsImV4cCI6MTk4MTQ5MDgwMX0.qUXjcOXhZJtYQX4q32YlCnppIpxbd8mf4x5-tA8tUpA"

                let! response = client.GetStringAsync(finalUrl)
                return response
                // client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InV4ZHNoY3R2eXBjam1qbXdxbmR3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE2NjU5MTQ4MDEsImV4cCI6MTk4MTQ5MDgwMX0.qUXjcOXhZJtYQX4q32YlCnppIpxbd8mf4x5-tA8tUpA")
                // client.DefaultRequestHeaders.Add("Bearer", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InV4ZHNoY3R2eXBjam1qbXdxbmR3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE2NjU5MTQ4MDEsImV4cCI6MTk4MTQ5MDgwMX0.qUXjcOXhZJtYQX4q32YlCnppIpxbd8mf4x5-tA8tUpA")
                // client.DefaultRequestHeaders.Add("apiKey", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InV4ZHNoY3R2eXBjam1qbXdxbmR3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE2NjU5MTQ4MDEsImV4cCI6MTk4MTQ5MDgwMX0.qUXjcOXhZJtYQX4q32YlCnppIpxbd8mf4x5-tA8tUpA")
                
            } |> Async.AwaitTask |> Async.RunSynchronously
            
        printfn $"{result}"
        // printfn $"{connection}"
    
    // let executeAsync () =
    //     task {
    //             use client = new HttpClient()
    //            
    //             // client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InV4ZHNoY3R2eXBjam1qbXdxbmR3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE2NjU5MTQ4MDEsImV4cCI6MTk4MTQ5MDgwMX0.qUXjcOXhZJtYQX4q32YlCnppIpxbd8mf4x5-tA8tUpA")
    //             client.DefaultRequestHeaders.Add("Bearer", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InV4ZHNoY3R2eXBjam1qbXdxbmR3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE2NjU5MTQ4MDEsImV4cCI6MTk4MTQ5MDgwMX0.qUXjcOXhZJtYQX4q32YlCnppIpxbd8mf4x5-tA8tUpA")
    //             client.DefaultRequestHeaders.Add("apiKey", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InV4ZHNoY3R2eXBjam1qbXdxbmR3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE2NjU5MTQ4MDEsImV4cCI6MTk4MTQ5MDgwMX0.qUXjcOXhZJtYQX4q32YlCnppIpxbd8mf4x5-tA8tUpA")
    //
    //             let! response = client.GetStringAsync(finalUrl)
    //             return response
    //         }