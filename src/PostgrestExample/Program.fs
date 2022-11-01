// open Postgrest
open Postgrest.Client
open Postgrest.Connection

// open Postgrest.StatelessClient

// StatelessClient.connect "url" "apiKey"
// |> StatelessClient.execute

// PostgrestClient.connect "url"
// PostgrestClient.execute ()

// PostgrestClient.connectWithHeaders
//     baseUrl
//     (
//      Map [
//         "apiKey", apiKey
//         "Bearer ", apiKey
//         ]
//      )

// PostgrestClient.execute ()

let baseUrl = "https://uxdshctvypcjmjmwqndw.supabase.co/rest/v1"
let apiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InV4ZHNoY3R2eXBjam1qbXdxbmR3Iiwicm9sZSI6ImFub24iLCJpYXQiOjE2NjU5MTQ4MDEsImV4cCI6MTk4MTQ5MDgwMX0.qUXjcOXhZJtYQX4q32YlCnppIpxbd8mf4x5-tA8tUpA"

let conn = postgrestConnection {
     url baseUrl
     headers (Map [ "apiKey", apiKey
                    "Bearer", apiKey ]
    )
}

type Test = {
    id: int
    name: string
}

// let result =
//     conn
//     |> from "test"
//     |> select None
//     // |> filter (EQ ("id" , Int 1))
//     // |> in_ ("id", [ 1; 2 ])
//     |> executeSelect<Test list>
//
// printfn $"{result}"

let testData =
    { id = 2
      name = "Anakin" }


let insertion =
    conn
    |> from "test"
    |> insert testData
    |> executeInsert


