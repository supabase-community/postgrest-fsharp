module ClientTests

open Xunit
open FsUnit.Xunit
open Postgrest.Client
open Postgrest.Common
open Postgrest.Connection

let _url = "https://xxx.supabase.co/rest/v1"
let _headers = (Map ["apiKey", "12345"])

let conn = postgrestConnection {
    url _url
    headers _headers
}

[<Fact>]
let ``Create connection with given url and headers`` () =
    conn.Url     |> should equal _url
    conn.Headers |> should equal _headers
    
[<Fact>]
let ``From should set up Table in returned Query`` () =
    let tableName = "table"
    let query = conn |> from tableName
    
    query.Table      |> should equal tableName
    query.Connection |> should equal conn
    
[<Fact>]
let ``Select adds ?select to QueryString and sets RequestType to Select`` () =
    let pfb =
        conn
        |> from "test"
        |> select All
        
    pfb.Query.QueryString |> should startWith "?select"
    pfb.RequestType       |> should equal Select
  
[<Fact>]
let ``Select All should select * from table`` () =
    let pfb =
        conn
        |> from "table"
        |> select All 
     
    pfb.Query.QueryString |> should equal "?select=*"
    
[<Fact>]
let ``Select with specific empty params list should select * from table`` () =
    let pfb =
        conn
        |> from "table"
        |> select (Cols []) 
     
    pfb.Query.QueryString |> should equal "?select=*"
    
[<Fact>]
let ``Select with specific params should select given params from table`` () =
    let usernameCol = "username"
    let passwordCol = "password"
    
    let pfb =
        conn
        |> from "table"
        |> select (Cols [usernameCol; passwordCol]) 
     
    pfb.Query.QueryString |> should equal $"?select={usernameCol},{passwordCol}"
    
[<Fact>]
let ``Delete adds ?delete to QueryString and sets RequestType to Delete`` () =
    let pfb =
        conn
        |> from "table"
        |> delete
    
    pfb.Query.QueryString |> should equal "?delete"
    pfb.RequestType       |> should equal Delete