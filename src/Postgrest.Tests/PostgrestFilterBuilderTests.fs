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
        let pfb =
            conn
            |> from ""
            |> select All
            |> filter (OpEqual ("", String ""))
            
        pfb.QueryFilterString.IsNone |> should be False
        
    [<Fact>]
    let ``Calling filter should add given filter to QueryFilterString`` () =
        let column = "column"
        let colName = "col_name"
        
        let pfb =
            conn
            |> from "table"
            |> select All
            |> filter (OpEqual (column, String colName))
            
        pfb.QueryFilterString |> should equal (Some $"&{column}=eq.{colName}")
        
    [<Fact>]
    let ``Calling filter multiple times should add all filters to QueryFilterString`` () =
        let column1 = "column"
        let colName1 = "col_name"
        
        let column2 = "column_2"
        
        let pfb =
            conn
            |> from "table"
            |> select All
            |> filter (OpGreaterThan (column1, String colName1))
            |> filter (OpEqual (column2, Bool true))
            
        pfb.QueryFilterString |> should equal (Some $"&{column1}=gt.{colName1}&{column2}=eq.true")
        
    [<Fact>]
    let ``Calling OpNot on filtering operator should prepend not. to QueryFilterString`` () =
        let column = "column"
        let colName = "col_name"
        
        let pfb =
            conn
            |> from "table"
            |> select All
            |> filter (OpNot (OpEqual (column, String colName)))
            
        pfb.QueryFilterString |> should equal (Some $"&not.{column}=eq.{colName}")
        
    [<Fact>]
    let ``Nested operator OpOr should combine given filters together`` () =
        let column1 = "column"
        let colName1 = "col_name"
        
        let column2 = "column_2"
        
        let pfb =
            conn
            |> from "table"
            |> select All
            |> filter (OpOr ((OpGreaterThan (column1, String colName1)), (OpEqual (column2, Bool true)))) 
            
        pfb.QueryFilterString |> should equal (Some $"&or=({column1}=gt.{colName1},{column2}=eq.true)")
        
module InFilterTests =
    let [<Literal>] column = "column"
    let [<Literal>] columnName1 = "col_n"
    let [<Literal>] columnName2 = "col_n_2"
            
    [<Fact>]
    let ``Calling in' filter with empty values should not change QueryInString`` () =
        let pfbBeforeIn =
            conn
            |> from "table"
            |> select All
            
        let pfbAfterIn = pfbBeforeIn |> in' (column, [])
        
        pfbBeforeIn.QueryInString |> should equal pfbAfterIn.QueryInString
        pfbAfterIn.QueryInString.IsNone |> should be True
        
    [<Fact>]
    let ``Calling in' filter with one value should add value to QueryInString`` () =
        let pfb =
            conn
            |> from "table"
            |> select All
            |> in' (column, [columnName1])
            
        pfb.QueryInString |> should equal (Some $"&{column}=in.({columnName1})")
        
    [<Fact>]
    let ``Calling in' filter with more values should add values to QueryInString`` () =
        let pfb =
            conn
            |> from "table"
            |> select All
            |> in' (column, [columnName1; columnName2])
            
        pfb.QueryInString |> should equal (Some $"&{column}=in.({columnName1},{columnName2})")