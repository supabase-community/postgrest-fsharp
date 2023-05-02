module ClientTests

open System.Net
open System.Net.Http
open System.Threading
open Moq
open Moq.Protected
open Xunit
open FsUnit.Xunit
open Postgrest
open Postgrest.Common
open Postgrest.Http

[<Fact>]
let ``postgrestConnection should create connection with given url and headers`` () =
    let connection = postgrestConnection {
        url "https://xxx.supabase.co/rest/v1"
        headers Map["apiKey", "exampleApiKey"]
    }
    let clientInfoKey, clientInfoValue = Connection.clientInfo
    
    connection.Url     |> should equal "https://xxx.supabase.co/rest/v1"
    connection.Headers |> should equal Map[ clientInfoKey, clientInfoValue
                                            "apiKey", "exampleApiKey" ]
    
[<Fact>]
let ``from should set up Table in returned Query`` () =
    // Arrange
    let connection = postgrestConnection {
        url "https://xxx.supabase.co/rest/v1"
        headers Map["apiKey", "exampleApiKey"]
    }
    
    let tableName = "table"
    
    // Act
    let query = connection |> from tableName
    
    // Assert
    query.Table      |> should equal tableName
    query.Connection |> should equal connection
    
[<Collection("select")>]
module SelectTests =
    [<Fact>]
    let ``select adds ?select to QueryString and sets RequestType to Select`` () =
        // Arrange
        let connection = postgrestConnection {
            url "https://xxx.supabase.co/rest/v1"
            headers Map["apiKey", "exampleApiKey"]
        }
        
        let table = "test"
        let columns = All
        
        // Act
        let pfb =
            connection
            |> from table
            |> select columns
            
        // Assert
        pfb.Query.QueryString |> should startWith "?select"
        pfb.RequestType       |> should equal Select
      
    [<Fact>]
    let ``select All should select * from table`` () =
        // Arrange
        let connection = postgrestConnection {
            url "https://xxx.supabase.co/rest/v1"
            headers Map["apiKey", "exampleApiKey"]
        }
        
        let table = "table"
        let columns = All
        
        // Act
        let pfb =
            connection
            |> from table
            |> select columns 
         
        // Assert
        pfb.Query.QueryString |> should equal "?select=*"
        
    [<Fact>]
    let ``select with specific empty params list should select * from table`` () =
        // Arrange
        let connection = postgrestConnection {
            url "https://xxx.supabase.co/rest/v1"
            headers Map["apiKey", "exampleApiKey"]
        }
        
        let table = "table"
        let columns = Columns []
        
        // Act
        let pfb =
            connection
            |> from table
            |> select columns 
         
        // Assert
        pfb.Query.QueryString |> should equal "?select=*"
        
    [<Fact>]
    let ``select with specific params should select given params from table`` () =
        // Arrange
        let connection = postgrestConnection {
            url "https://xxx.supabase.co/rest/v1"
            headers Map["apiKey", "exampleApiKey"]
        }
        let table = "table"
        let usernameCol = "username"
        let passwordCol = "password"
        
        // Act
        let pfb =
            connection
            |> from table
            |> select (Columns [usernameCol ; passwordCol]) 
         
        // Assert
        pfb.Query.QueryString |> should equal $"?select={usernameCol},{passwordCol}"
    
[<Fact>]
let ``delete adds ?delete to QueryString and sets RequestType to Delete`` () =
    // Arrange
    let connection = postgrestConnection {
        url "https://xxx.supabase.co/rest/v1"
        headers Map["apiKey", "exampleApiKey"]
    }
    
    let table = "table"
    
    // Act
    let pfb =
        connection
        |> from table
        |> Client.delete
    
    // Assert
    pfb.Query.QueryString |> should equal "?delete"
    pfb.RequestType       |> should equal Delete
    
[<Fact>]
let ``update adds ?update to QueryString and sets RequestType to Update`` () =
    // Arrange
    let connection = postgrestConnection {
        url "https://xxx.supabase.co/rest/v1"
        headers Map["apiKey", "exampleApiKey"]
    }
    
    let table = "table"
    let expectedBody = Some """[]"""
    
    // Act
    let pfb =
        connection
        |> from table
        |> update []
    
    // Assert
    pfb.Query.QueryString |> should equal "?update"
    pfb.RequestType       |> should equal Update
    pfb.Body              |> should equal expectedBody
    
[<Fact>]
let ``insert adds ?insert to QueryString`` () =
    // Arrange
    let connection = postgrestConnection {
        url "https://xxx.supabase.co/rest/v1"
        headers Map["apiKey", "exampleApiKey"]
    }
    
    let table = "table"
    let expectedBody = """[]"""
    
    // Act
    let pfb =
        connection
        |> from table
        |> insert []
    
    // Assert
    pfb.Query.QueryString |> should equal "?insert"
    pfb.Body              |> should equal expectedBody

[<Collection("execute")>]
module ExecuteTests =
    [<Collection("executeSelect")>]
    module ExecuteSelectTest =
        type SelectResponse = {
            id: string
            name: string
        }
        
        [<Fact>]
        let ``execute with Select RequestType should successfully return a response of given type (SelectResponse)`` () =
            // Arrange
            let response =
                """{
                    "id": "test-id",
                    "name": "test-name"
                }"""
            let expectedResponse = { id = "test-id" ; name = "test-name" }
            
            let mockHandler = mockHttpMessageHandler response
            let mockHttpClient = new HttpClient(mockHandler.Object)
            
            let connection = postgrestConnection {
                url "http://example.com"
                headers Map["apiKey", "exampleApiKey"]
                httpClient mockHttpClient
            }
            
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
            let result = PostgrestFilterBuilder.execute<SelectResponse> pfb |> Async.RunSynchronously

            // Assert
            match result with
            | Ok bucket -> bucket |> should equal expectedResponse
            | Error err -> failwithf $"Expected Ok, but got Error: {err}"
            
            // Verify
            mockHandler.Protected()
                .Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            mockHandler.Protected()
                .Verify("SendAsync", Times.Once(), 
                    ItExpr.Is<HttpRequestMessage>(fun req ->
                        req.Method = HttpMethod.Get &&
                        req.Headers.Contains("apiKey") &&
                        req.RequestUri.AbsoluteUri = "http://example.com/table?select=*"),
                    ItExpr.IsAny<CancellationToken>()
                )
                
        [<Fact>]
        let ``execute with Select RequestType should return PostgrestError when API call is not successful`` () =
            // Arrange
            let expectedError = { message = "Bad Request"; statusCode = Some HttpStatusCode.BadRequest }
            
            let mockHandler = mockHttpMessageHandlerFail expectedError
            let mockHttpClient = new HttpClient(mockHandler.Object)
            
            let connection = postgrestConnection {
                url "http://example.com"
                headers Map["apiKey", "exampleApiKey"]
                httpClient mockHttpClient
            }
            
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
            let result = PostgrestFilterBuilder.execute<SelectResponse> pfb |> Async.RunSynchronously

            // Assert
            match result with
            | Ok ok -> failwithf $"Expected Error, but got Ok: {ok}"
            | Error err -> err |> should equal expectedError
            
            // Verify
            mockHandler.Protected()
                .Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            mockHandler.Protected()
                .Verify("SendAsync", Times.Once(), 
                    ItExpr.Is<HttpRequestMessage>(fun req ->
                        req.Method = HttpMethod.Get &&
                        req.Headers.Contains("apiKey") &&
                        req.RequestUri.AbsoluteUri = "http://example.com/table?select=*"),
                    ItExpr.IsAny<CancellationToken>()
                )
    [<Collection("executeDelete")>]
    module ExecuteDeleteTest =
        type DeleteResponse = {
            id: string
            name: string
        }
        
        [<Fact>]
        let ``execute with Delete RequestType should successfully return a response of given type (DeleteResponse)`` () =
            // Arrange
            let response =
                """{
                    "id": "test-id",
                    "name": "test-name"
                }"""
            let expectedResponse = { id = "test-id" ; name = "test-name" }
            
            let mockHandler = mockHttpMessageHandler response
            let mockHttpClient = new HttpClient(mockHandler.Object)
            
            let connection = postgrestConnection {
                url "http://example.com"
                headers Map["apiKey", "exampleApiKey"]
                httpClient mockHttpClient
            }
            
            let pfb =
                { Query = { Connection = connection ; Table = "table" ; QueryString = "?delete" }
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
                  RequestType       = Delete }
            
            // Act
            let result = PostgrestFilterBuilder.execute<DeleteResponse> pfb |> Async.RunSynchronously

            // Assert
            match result with
            | Ok bucket -> bucket |> should equal expectedResponse
            | Error err -> failwithf $"Expected Ok, but got Error: {err}"
            
            // Verify
            mockHandler.Protected()
                .Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            mockHandler.Protected()
                .Verify("SendAsync", Times.Once(), 
                    ItExpr.Is<HttpRequestMessage>(fun req ->
                        req.Method = HttpMethod.Delete &&
                        req.Headers.Contains("apiKey") &&
                        req.RequestUri.AbsoluteUri = "http://example.com/table?delete"),
                    ItExpr.IsAny<CancellationToken>()
                )
                
        [<Fact>]
        let ``execute with Delete RequestType should return PostgrestError when API call is not successful`` () =
            // Arrange
            let expectedError = { message = "Bad Request"; statusCode = Some HttpStatusCode.BadRequest }
            
            let mockHandler = mockHttpMessageHandlerFail expectedError
            let mockHttpClient = new HttpClient(mockHandler.Object)
            
            let connection = postgrestConnection {
                url "http://example.com"
                headers Map["apiKey", "exampleApiKey"]
                httpClient mockHttpClient
            }
            
            let pfb =
                { Query = { Connection = connection ; Table = "table" ; QueryString = "?delete" }
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
                  RequestType       = Delete }
            
            // Act
            let result = PostgrestFilterBuilder.execute<DeleteResponse> pfb |> Async.RunSynchronously

            // Assert
            match result with
            | Ok ok -> failwithf $"Expected Error, but got Ok: {ok}"
            | Error err -> err |> should equal expectedError
            
            // Verify
            mockHandler.Protected()
                .Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            mockHandler.Protected()
                .Verify("SendAsync", Times.Once(), 
                    ItExpr.Is<HttpRequestMessage>(fun req ->
                        req.Method = HttpMethod.Delete &&
                        req.Headers.Contains("apiKey") &&
                        req.RequestUri.AbsoluteUri = "http://example.com/table?delete"),
                    ItExpr.IsAny<CancellationToken>()
                )
                
    [<Collection("executeUpdate")>]
    module ExecuteUpdateTest =
        type UpdateResponse = {
            id: string
            name: string
        }
        
        [<Fact>]
        let ``execute with Update RequestType should successfully return a response of given type (UpdateResponse)`` () =
            // Arrange
            let response =
                """{
                    "id": "test-id",
                    "name": "test-name"
                }"""
            let expectedResponse = { id = "test-id" ; name = "test-name" }
            
            let requestBody =
                """{
                    "id": "test-id",
                    "name": "test-name-new"
                }"""
            
            let mockHandler = mockHttpMessageHandler response
            let mockHttpClient = new HttpClient(mockHandler.Object)
            
            let connection = postgrestConnection {
                url "http://example.com"
                headers Map["apiKey", "exampleApiKey"]
                httpClient mockHttpClient
            }
            
            let pfb =
                { Query = { Connection = connection ; Table = "table" ; QueryString = "?update" }
                  QueryFilterString = None
                  QueryInString     = None
                  QueryIsString     = None
                  QueryOrderString  = None
                  QueryLimitString  = None
                  QueryOffsetString = None
                  QueryLikeString   = None
                  QueryILikeString  = None
                  QueryFtsString    = None
                  Body              = Some requestBody
                  RequestType       = Update }
            
            // Act
            let result = PostgrestFilterBuilder.execute<UpdateResponse> pfb |> Async.RunSynchronously 

            // Assert
            match result with
            | Ok bucket -> bucket |> should equal expectedResponse
            | Error err -> failwithf $"Expected Ok, but got Error: {err}"
            
            // Verify
            mockHandler.Protected()
                .Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            mockHandler.Protected()
                .Verify("SendAsync", Times.Once(), 
                    ItExpr.Is<HttpRequestMessage>(fun req ->
                        req.Method = HttpMethod.Patch &&
                        req.Headers.Contains("apiKey") &&
                        req.RequestUri.AbsoluteUri = "http://example.com/table?update" &&
                        req.Content.ReadAsStringAsync().Result = requestBody),
                    ItExpr.IsAny<CancellationToken>()
                )
                
        [<Fact>]
        let ``execute with Update RequestType should return PostgrestError when API call is not successful`` () =
            // Arrange
            let expectedError = { message = "Bad Request"; statusCode = Some HttpStatusCode.BadRequest }
            
            let requestBody =
                """{
                    "id": "test-id",
                    "name": "test-name-new"
                }"""
            
            let mockHandler = mockHttpMessageHandlerFail expectedError
            let mockHttpClient = new HttpClient(mockHandler.Object)
            
            let connection = postgrestConnection {
                url "http://example.com"
                headers Map["apiKey", "exampleApiKey"]
                httpClient mockHttpClient
            }
            
            let pfb =
                { Query = { Connection = connection ; Table = "table" ; QueryString = "?update" }
                  QueryFilterString = None
                  QueryInString     = None
                  QueryIsString     = None
                  QueryOrderString  = None
                  QueryLimitString  = None
                  QueryOffsetString = None
                  QueryLikeString   = None
                  QueryILikeString  = None
                  QueryFtsString    = None
                  Body              = Some requestBody
                  RequestType       = Update }
            
            // Act
            let result = PostgrestFilterBuilder.execute<UpdateResponse> pfb |> Async.RunSynchronously

            // Assert
            match result with
            | Ok ok -> failwithf $"Expected Error, but got Ok: {ok}"
            | Error err -> err |> should equal expectedError
            
            // Verify
            mockHandler.Protected()
                .Verify("SendAsync", Times.Once(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            mockHandler.Protected()
                .Verify("SendAsync", Times.Once(), 
                    ItExpr.Is<HttpRequestMessage>(fun req ->
                        req.Method = HttpMethod.Patch &&
                        req.Headers.Contains("apiKey") &&
                        req.RequestUri.AbsoluteUri = "http://example.com/table?update" &&
                        req.Content.ReadAsStringAsync().Result = requestBody),
                    ItExpr.IsAny<CancellationToken>()
                )