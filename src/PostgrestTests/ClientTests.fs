module ClientTests

open Xunit
open FsUnit.Xunit
open Postgrest.Client
open Postgrest.Common
open Postgrest.Connection

let _url = "https://xxx.supabase.co/rest/v1"
let _headers = Map [ "apiKey", "12345" ]

let conn = postgrestConnection {
    url _url
    headers _headers
}

[<Fact>]
let ``Create connection with given url and headers`` () =
    conn.Url     |> should equal _url
    conn.Headers |> should equal _headers
    
[<Fact>]
let ``from should set up Table in returned Query`` () =
    // Arrange
    let tableName = "table"
    
    // Act
    let query = conn |> from tableName
    
    // Assert
    query.Table      |> should equal tableName
    query.Connection |> should equal conn
    
[<Collection("select")>]
module SelectTests =
    [<Fact>]
    let ``adds ?select to QueryString and sets RequestType to Select`` () =
        // Arrange
        let table = "test"
        let columns = All
        
        // Act
        let pfb =
            conn
            |> from table
            |> select columns
            
        // Assert
        pfb.Query.QueryString |> should startWith "?select"
        pfb.RequestType       |> should equal Select
      
    [<Fact>]
    let ``All should select * from table`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        // Act
        let pfb =
            conn
            |> from table
            |> select columns 
         
        // Assert
        pfb.Query.QueryString |> should equal "?select=*"
        
    [<Fact>]
    let ``with specific empty params list should select * from table`` () =
        // Arrange
        let table = "table"
        let columns = Cols []
        
        // Act
        let pfb =
            conn
            |> from table
            |> select columns 
         
        // Assert
        pfb.Query.QueryString |> should equal "?select=*"
        
    [<Fact>]
    let ``with specific params should select given params from table`` () =
        // Arrange
        let table = "table"
        let usernameCol = "username"
        let passwordCol = "password"
        
        // Act
        let pfb =
            conn
            |> from table
            |> select (Cols [usernameCol ; passwordCol]) 
         
        // Assert
        pfb.Query.QueryString |> should equal $"?select={usernameCol},{passwordCol}"
    
[<Fact>]
let ``delete adds ?delete to QueryString and sets RequestType to Delete`` () =
    // Arrange
    let table = "table"
    
    // Act
    let pfb =
        conn
        |> from table
        |> delete
    
    // Assert
    pfb.Query.QueryString |> should equal "?delete"
    pfb.RequestType       |> should equal Delete
    
[<Fact>]
let ``update adds ?update to QueryString and sets RequestType to Update`` () =
    // Arrange
    let table = "table"
    let expectedBody = Some """[]"""
    
    // Act
    let pfb =
        conn
        |> from table
        |> update []
    
    // Assert
    pfb.Query.QueryString |> should equal "?update"
    pfb.RequestType       |> should equal Update
    pfb.Body              |> should equal expectedBody
    
[<Fact>]
let ``inset adds ?insert to QueryString`` () =
    // Arrange
    let table = "table"
    let expectedBody = """[]"""
    
    // Act
    let pfb =
        conn
        |> from table
        |> insert []
    
    // Assert
    pfb.Query.QueryString |> should equal "?insert"
    pfb.Body              |> should equal expectedBody