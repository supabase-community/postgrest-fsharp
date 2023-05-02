module PostgrestFilterBuilderHelperTests

open FsUnit.Xunit
open Xunit
open Postgrest
open Postgrest.Common

[<Collection("getUrlSuffixFromPostgresFilterBuilder")>]
module GetUrlSuffixFromPostgresFilterBuilderTests =
    [<Fact>]
    let ``returns string with table name if none of PostgrestFilterBuilder is set`` () =
        // Arrange
        let connection = postgrestConnection {
            url "https://xxx.supabase.co/rest/v1"
            headers  (Map [ "apiKey", "exampleApiKey" ])
        }
    
        let expectedResult = "table?select=*"
        let pfb =
            { Query = { Connection = connection ; Table = "table" ; QueryString = "?select=*" }
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
        let result = PostgrestFilterBuilderHelper.getUrlSuffixFromPostgresFilterBuilder pfb 

        // Assert
        result |> should equal expectedResult
        
    [<Fact>]
    let ``returns string with table name and all of given PostgrestFilterBuilder params`` () =
        // Arrange
        let connection = postgrestConnection {
            url "https://xxx.supabase.co/rest/v1"
            headers  (Map [ "apiKey", "exampleApiKey" ])
        }
         
        let expectedResult = "table?select=*&filter=f&in=in&is=is&order=o&limit=l&offset=o&like=l&ilike=i&fts=f"
        let pfb =
            { Query = { Connection = connection ; Table = "table" ; QueryString = "?select=*" }
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
        let result = PostgrestFilterBuilderHelper.getUrlSuffixFromPostgresFilterBuilder pfb 

        // Assert
        result |> should equal expectedResult

