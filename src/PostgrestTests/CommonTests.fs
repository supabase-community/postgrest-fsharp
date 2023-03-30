module CommonTests

open FsUnit.Xunit
open Postgrest
open Postgrest.Connection
open Xunit


[<Collection("parseColumns")>]
module ParseColumnsTests =
    [<Fact>]
    let ``returns * if columns is All`` () =
        // Arrange
        let expectedResult = "*"
        let columns = All

        // Act
        let result = Common.parseColumns columns 

        // Assert
        result |> should equal expectedResult
        
    [<Fact>]
    let ``returns * if columns is Cols with empty list`` () =
        // Arrange
        let expectedResult = "*"
        let columns = Cols []

        // Act
        let result = Common.parseColumns columns 

        // Assert
        result |> should equal expectedResult
        
    [<Fact>]
    let ``returns given columns if columns is Cols with non empty list`` () =
        // Arrange
        let expectedResult = "col-1,col-2"
        let columns = Cols [ "col-1" ; "col-2"]

        // Act
        let result = Common.parseColumns columns 

        // Assert
        result |> should equal expectedResult
        
[<Collection("getUrlSuffixFromPostgresFilterBuilder")>]
module GetUrlSuffixFromPostgresFilterBuilderTests =
    let _url = "https://xxx.supabase.co/rest/v1"
    let _headers = Map [ "apiKey", "12345" ]

    let conn = postgrestConnection {
        url _url
        headers _headers
    }
    
    [<Fact>]
    let ``returns string with table name if none of PostgrestFilterBuilder is set`` () =
        // Arrange
        let expectedResult = "?table-name=name"
        let pfb =
            { Query = { Connection = conn ; Table = "?table-name=name" ; QueryString = "" }
              QueryFilterString = None
              QueryInString     = None
              QueryIsString     = None
              QueryOrderString  = None
              QueryLimitString  = None
              QueryOffsetString = None
              QueryLikeString   = None
              QueryILikeString  = None
              QueryFtsString    = None
              Body              = None
              RequestType       = Select }

        // Act
        let result = Common.getUrlSuffixFromPostgresFilterBuilder pfb 

        // Assert
        result |> should equal expectedResult
        
    [<Fact>]
    let ``returns string with table name and all of given PostgrestFilterBuilder params`` () =
        // Arrange
        let expectedResult = "?table-name=name&filter=f&in=in&is=is&order=o&limit=l&offset=o&like=l&ilike=i&fts=f"
        let pfb =
            { Query = { Connection = conn ; Table = "?table-name=name" ; QueryString = "" }
              QueryFilterString = Some "&filter=f"
              QueryInString     = Some "&in=in"
              QueryIsString     = Some "&is=is"
              QueryOrderString  = Some "&order=o"
              QueryLimitString  = Some "&limit=l"
              QueryOffsetString = Some "&offset=o"
              QueryLikeString   = Some "&like=l"
              QueryILikeString  = Some "&ilike=i"
              QueryFtsString    = Some "&fts=f"
              Body              = None
              RequestType       = Select }

        // Act
        let result = Common.getUrlSuffixFromPostgresFilterBuilder pfb 

        // Assert
        result |> should equal expectedResult