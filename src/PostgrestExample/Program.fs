open Postgrest
open Postgrest.Common

let baseUrl = "https://<project-id>.supabase.co/rest/v1"
let apiKey = "<api-key>"

let connection = postgrestConnection {
     url baseUrl
     headers (Map [ "apiKey", apiKey
                    "Authorization", $"Bearer {apiKey}" ] )
}

type Test = {
    id: int
    name: string
}

let result =
    connection
    |> from "test"
    |> select All
    |> filter (OpLessThanEqual ("id", (Int 5)))
    |> order [("id" ,(Some OrderType.Ascending), None)]
    |> limit 3
    |> PostgrestFilterBuilder.execute<Test list>
    |> Async.RunSynchronously

match result with
| Ok r    -> printf $"{r}"
| Error e -> printfn $"{e}"