module PostgrestFilterBuilderTests

open Postgrest
open Postgrest.Connection
open FsUnit.Xunit
open Xunit

let _url = "https://xxx.supabase.co/rest/v1"
let _headers = Map ["apiKey", "12345"]

let conn = postgrestConnection {
    url _url
    headers _headers
}

module FilterTests = 
    [<Fact>]
    let ``Calling filter on pfb should lead to QueryFilterString not being None`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        let filterOp = (OpEqual ("", String ""))
        
        // Act
        let pfb =
            conn
            |> from table
            |> select columns
            |> filter filterOp
            
        // Assert
        pfb.QueryFilterString.IsNone |> should be False
        
    [<Fact>]
    let ``Calling filter should add given filter to QueryFilterString`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        let column = "column"
        let columnName = "column-name"
        
        // Act
        let pfb =
            conn
            |> from table
            |> select columns
            |> filter (OpEqual (column, String columnName))
            
        // Assert
        pfb.QueryFilterString |> should equal (Some $"&{column}=eq.{columnName}")
        
    [<Fact>]
    let ``Calling filter multiple times should add all filters to QueryFilterString`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        let column1 = "column"
        let columnValue = "column-value"
        let column2 = "column-2"
        
        // Act
        let pfb =
            conn
            |> from table
            |> select columns
            |> filter (OpGreaterThan (column1, String columnValue))
            |> filter (OpEqual (column2, Bool true))
            
        // Assert
        pfb.QueryFilterString |> should equal (Some $"&{column1}=gt.{columnValue}&{column2}=eq.true")
        
    [<Fact>]
    let ``Calling OpNot on filtering operator should prepend not. to QueryFilterString`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        let column = "column"
        let columnValue = "column-value"
        
        // Act
        let pfb =
            conn
            |> from table
            |> select columns
            |> filter (OpNot (OpEqual (column, String columnValue)))
            
        // Assert
        pfb.QueryFilterString |> should equal (Some $"&not.{column}=eq.{columnValue}")
        
    [<Fact>]
    let ``Nested operator OpOr should combine given filters together`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        let column1 = "column"
        let columnValue = "column-value"
        let column2 = "column-2"
        
        // Act
        let pfb =
            conn
            |> from table
            |> select columns
            |> filter (OpOr ((OpGreaterThan (column1, String columnValue)), (OpEqual (column2, Bool true)))) 
            
        // Assert
        pfb.QueryFilterString |> should equal (Some $"&or=({column1}=gt.{columnValue},{column2}=eq.true)")
        
module InFilterTests =
    let [<Literal>] column = "column"
    let [<Literal>] columnValue1 = "column-value-1"
    let [<Literal>] columnValue2 = "column-value-2"
            
    [<Fact>]
    let ``Calling in' filter with empty values should not change QueryInString`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        // Act
        let pfbBeforeIn =
            conn
            |> from table
            |> select columns
        let pfbAfterIn = pfbBeforeIn |> in' (column, [])
        
        // Assert
        pfbBeforeIn.QueryInString |> should equal pfbAfterIn.QueryInString
        pfbAfterIn.QueryInString.IsNone |> should be True
        
    [<Fact>]
    let ``Calling in' filter with one value should add value to QueryInString`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        // Act
        let pfb =
            conn
            |> from table
            |> select columns
            |> in' (column, [columnValue1])
            
        // Assert
        pfb.QueryInString |> should equal (Some $"&{column}=in.({columnValue1})")
        
    [<Fact>]
    let ``Calling in' filter with more values should add values to QueryInString`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        // Act
        let pfb =
            conn
            |> from table
            |> select columns
            |> in' (column, [columnValue1; columnValue2])
            
        // Assert
        pfb.QueryInString |> should equal (Some $"&{column}=in.({columnValue1},{columnValue2})")