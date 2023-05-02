open Postgrest
open Postgrest.Common

let baseUrl = "https://<url>.supabase.co/rest/v1"
let apiKey = "<api-key>"

let conn = postgrestConnection {
     url baseUrl
     headers (Map [ "apiKey", apiKey
                    "Authorization", $"Bearer {apiKey}" ] )
}

type Test = {
    id: int
    name: string
}

let result =
    conn
    |> from "test"
    |> select All
    |> PostgrestFilterBuilder.execute<Test list>
    |> Async.RunSynchronously

match result with
| Ok r    -> printf $"{r}"
| Error e -> printfn $"{e}"