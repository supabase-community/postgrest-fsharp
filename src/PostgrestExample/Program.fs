// For more information see https://aka.ms/fsharp-console-apps
// printfn "Hello from F#"

open Postgrest
open Postgrest.StatelessClient

// StatelessClient.connect "url" "apiKey"
// |> StatelessClient.execute

printfn "\n"

PostgrestClient.connect "url"
PostgrestClient.execute ()

PostgrestClient.connectWithHeaders "url" (Some(Map ["apiKey", "fasfasf"]))
PostgrestClient.execute ()

