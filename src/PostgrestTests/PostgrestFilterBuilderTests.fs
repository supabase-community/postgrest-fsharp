module PostgrestFilterBuilderTests

open Postgrest
open Postgrest.Connection
open FsUnit.Xunit
open Xunit

let connection = postgrestConnection {
    url "https://xxx.supabase.co/rest/v1"
    headers (Map ["apiKey", "exampleApiKey"])
}

[<Collection("filter")>]
module FilterTests = 
    [<Fact>]
    let ``filter should make input pfb to have QueryFilterString parameter not being None`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        let filterOp = (OpEqual ("", String ""))
        
        // Act
        let pfb =
            connection
            |> from table
            |> select columns
            |> filter filterOp
            
        // Assert
        pfb.QueryFilterString.IsNone |> should be False
        
    [<Fact>]
    let ``filter should add given filter to QueryFilterString`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        let column = "column"
        let columnName = "column-name"
        
        // Act
        let pfb =
            connection
            |> from table
            |> select columns
            |> filter (OpEqual (column, String columnName))
            
        // Assert
        pfb.QueryFilterString |> should equal (Some $"&{column}=eq.{columnName}")
        
    [<Fact>]
    let ``filter called multiple times should add all filters to QueryFilterString`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        let column1 = "column"
        let columnValue = "column-value"
        let column2 = "column-2"
        
        // Act
        let pfb =
            connection
            |> from table
            |> select columns
            |> filter (OpGreaterThan (column1, String columnValue))
            |> filter (OpEqual (column2, Bool true))
            
        // Assert
        pfb.QueryFilterString |> should equal (Some $"&{column1}=gt.{columnValue}&{column2}=eq.true")
        
    [<Fact>]
    let ``filter with OpNot operator should prepend not. to QueryFilterString`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        let column = "column"
        let columnValue = "column-value"
        
        // Act
        let pfb =
            connection
            |> from table
            |> select columns
            |> filter (OpNot (OpEqual (column, String columnValue)))
            
        // Assert
        pfb.QueryFilterString |> should equal (Some $"&not.{column}=eq.{columnValue}")
        
    [<Fact>]
    let ``filter with nested operator OpOr should combine given filters together`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        let column1 = "column"
        let columnValue = "column-value"
        let column2 = "column-2"
        
        // Act
        let pfb =
            connection
            |> from table
            |> select columns
            |> filter (OpOr ((OpGreaterThan (column1, String columnValue)), (OpEqual (column2, Bool true)))) 
            
        // Assert
        pfb.QueryFilterString |> should equal (Some $"&or=({column1}=gt.{columnValue},{column2}=eq.true)")
        
[<Collection("in'")>]
module InFilterTests =
    let [<Literal>] column = "column"
    let [<Literal>] columnValue1 = "column-value-1"
    let [<Literal>] columnValue2 = "column-value-2"
            
    [<Fact>]
    let ``in' filter with empty values should not change QueryInString`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        // Act
        let pfbBeforeIn =
            connection
            |> from table
            |> select columns
        let pfbAfterIn = pfbBeforeIn |> in' (column, [])
        
        // Assert
        pfbAfterIn.QueryInString.IsNone |> should be True
        pfbBeforeIn.QueryInString |> should equal pfbAfterIn.QueryInString
        
    [<Fact>]
    let ``in' filter with one value should add value to QueryInString`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        // Act
        let pfb =
            connection
            |> from table
            |> select columns
            |> in' (column, [columnValue1])
            
        // Assert
        pfb.QueryInString |> should equal (Some $"&{column}=in.({columnValue1})")
        
    [<Fact>]
    let ``in' filter with more values should add values to QueryInString`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        // Act
        let pfb =
            connection
            |> from table
            |> select columns
            |> in' (column, [columnValue1; columnValue2])
            
        // Assert
        pfb.QueryInString |> should equal (Some $"&{column}=in.({columnValue1},{columnValue2})")
        
[<Collection("is")>]
module IsFilterTests =
    let [<Literal>] column = "column"
    
    [<Fact>]
    let ``is filter with IsNull should add is.null to QueryIsString`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        // Act
        let pfb =
            connection
            |> from table
            |> select columns
            |> is (column, IsNull)
        
        // Assert
        pfb.QueryIsString.IsSome |> should be True
        pfb.QueryIsString |> should equal (Some "&column=is.null")
        
    [<Fact>]
    let ``is filter with multiple values should NOT add all values to QueryIsString but keep latest one`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        // Act
        let pfb =
            connection
            |> from table
            |> select columns
            |> is (column, IsTrue)
            |> is (column, IsUnknown)
        
        // Assert
        pfb.QueryIsString.IsSome |> should be True
        pfb.QueryIsString |> should equal (Some "&column=is.unknown")
        
[<Collection("like")>]
module LikeFilterTests =
    let [<Literal>] column = "column"
    
    [<Fact>]
    let ``like filter should add filter to QueryLikeString`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        // Act
        let pfb =
            connection
            |> from table
            |> select columns
            |> like (column, "pattern")
        
        // Assert
        pfb.QueryLikeString.IsSome |> should be True
        pfb.QueryLikeString |> should equal (Some "&column=like.pattern")
        
[<Collection("ilike")>]
module ILikeFilterTests =
    let [<Literal>] column = "column"
    
    [<Fact>]
    let ``like filter should add filter to QueryLikeString`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        // Act
        let pfb =
            connection
            |> from table
            |> select columns
            |> ilike (column, "pattern")
        
        // Assert
        pfb.QueryILikeString.IsSome |> should be True
        pfb.QueryILikeString |> should equal (Some "&column=ilike.pattern")
        
[<Collection("order")>]
module OrderFilterTests =
    [<Fact>]
    let ``order filter with empty list should not affect QueryOrderString`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        // Act
        let pfbBeforeOrder =
            connection
            |> from table
            |> select columns
        
        let pfbAfterOrder = pfbBeforeOrder |> order []
        
        // Assert
        pfbBeforeOrder.QueryOrderString |> should equal pfbAfterOrder.QueryOrderString
        
    [<Fact>]
    let ``order filter with one item with no OrderType and no OrderNull should add this item to QueryOrderString`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        // Act
        let pfb =
            connection
            |> from table
            |> select columns
            |> order [("column", None, None)]
        
        // Assert
        pfb.QueryOrderString.IsSome |> should be True
        pfb.QueryOrderString |> should equal (Some "&order=column")
        
    [<Fact>]
    let ``order filter with one item with OrderType and OrderNull should add this item to QueryOrderString`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        // Act
        let pfb =
            connection
            |> from table
            |> select columns
            |> order [("column", Some Descending, Some NullLast)]
        
        // Assert
        pfb.QueryOrderString.IsSome |> should be True
        pfb.QueryOrderString |> should equal (Some "&order=column.desc.nullslast")
        
    [<Fact>]
    let ``order filter with multiple different items should add all these items to QueryOrderString`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        // Act
        let pfb =
            connection
            |> from table
            |> select columns
            |> order [ ("column1", Some Descending, Some NullLast)
                       ("column2", None, Some NullFirst)
                       ("column3", None, None)
                       ("column4", None, Some NullLast) ]
        
        // Assert
        pfb.QueryOrderString.IsSome |> should be True
        pfb.QueryOrderString
        |> should equal (Some "&order=column1.desc.nullslast,column2.nullsfirst,column3,column4.nullslast")
        
[<Collection("limit")>]
module LimitFilterTests =
    [<Fact>]
    let ``limit filter should add given limit to QueryLimitString`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        // Act
        let pfb =
            connection
            |> from table
            |> select columns
            |> limit 10
        
        // Assert
        pfb.QueryLimitString.IsSome |> should be True
        pfb.QueryLimitString |> should equal (Some "&limit=10")
        
[<Collection("offset")>]
module OffsetFilterTests =
    [<Fact>]
    let ``offset filter should add given limit to QueryOffsetString`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        // Act
        let pfb =
            connection
            |> from table
            |> select columns
            |> offset 20
        
        // Assert
        pfb.QueryOffsetString.IsSome |> should be True
        pfb.QueryOffsetString |> should equal (Some "&offset=20")
        
[<Collection("fts")>]
module FtsTests =
    [<Fact>]
    let ``fts should return fts keyword when ftsParam has empty column and FullTextSearch`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        // Act
        let pfbBeforeFts =
            connection
            |> from table
            |> select columns
        
        let pfbAfterFts = pfbBeforeFts |> fts ("", Fts ([], None))
        
        // Assert
        pfbAfterFts.QueryFtsString |> should equal (Some "&=fts.")
        
    [<Fact>]
    let ``fts should add fts params to QueryFtsString`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        // Act
        let pfb =
            connection
            |> from table
            |> select columns
            |> fts ("column", Fts (["text-1" ; "text-2"], None))
        
        // Assert
        pfb.QueryFtsString |> should equal (Some "&column=fts.text-1%20text-2")
        
    [<Fact>]
    let ``fts with FtsNot should be prepended to given fts params to QueryFtsString`` () =
        // Arrange
        let table = "table"
        let columns = All
        
        // Act
        let pfb =
            connection
            |> from table
            |> select columns
            |> fts ("column", FtsNot (Fts (["text-1" ; "text-2"], None)))
        
        // Assert
        pfb.QueryFtsString |> should equal (Some "&column=not.fts.text-1%20text-2")