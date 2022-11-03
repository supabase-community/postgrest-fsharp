open Postgrest.Client
open Postgrest.Connection
open Postgrest.QueryFilter

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

type Director = {
    id: int
}

type Film = {
    id: int
    title: string
    director: Director
}

let result =
    conn
    |> from "films"
    |> select (COLS ["title"; "director:directors(id,last_name)"])
    // |> filter (EQ ("id", Int 1)) 
    // |> select (Some ["id" ; "title" ; "director(id)"])
    // |> in_ ("id", [ 1; 2 ])
    |> executeSelect<Film list>

printfn $"{result}"

// let testData =
//     [ { id = 3 ; name = "Vader" }
//       { id = 4 ; name = "Obivan" } ]
//     
// let insertion =
//     conn
//     |> from "test"
//     |> insert testData
//     |> executeInsert