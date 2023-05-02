namespace Postgrest

open System.Net
open System.Net.Http
open FSharp.Json
open Postgrest.Common
open Postgrest.Connection

/// Contains functions for performing http request and serialization/deserialization of data
module Http =
    /// Represents error for postgrest
    type PostgrestError = {
        message: string
        statusCode: HttpStatusCode option
    }
    
    /// Parses HttpResponseMessage to it's string form
    let private getResponseBody (responseMessage: HttpResponseMessage): string = 
        responseMessage.Content.ReadAsStringAsync()
        |> Async.AwaitTask
        |> Async.RunSynchronously
            
    /// Deserializes given response
    let deserializeResponse<'T> (response: Result<HttpResponseMessage, PostgrestError>): Result<'T, PostgrestError> =        
        try
            match response with
            | Ok r    -> Result.Ok (Json.deserialize<'T> (getResponseBody r))
            | Error e -> Result.Error e
        with e -> Error { message = e.Message ; statusCode = None }
        
    /// Deserializes empty (unit) response
    let deserializeEmptyResponse (response: Result<HttpResponseMessage, PostgrestError>): Result<unit, PostgrestError> =
        match response with
        | Ok _    -> Result.Ok ()
        | Error e -> Result.Error e
        
    /// Executes http response with given headers, requestMessage and handles possible exceptions
    let executeHttpRequest (requestMessage: HttpRequestMessage) (connection: PostgrestConnection)
                           : Async<Result<HttpResponseMessage, PostgrestError>> =
        async {
            try
                let httpClient = connection.HttpClient
                addRequestHeaders connection.Headers requestMessage.Headers
                
                let! response = httpClient.SendAsync(requestMessage) |> Async.AwaitTask
                match response.StatusCode with
                | HttpStatusCode.OK | HttpStatusCode.Created -> return Result.Ok response
                | statusCode -> return Result.Error { message = getResponseBody response; statusCode = Some statusCode }
            with e -> return Result.Error { message = e.ToString(); statusCode = None }
        }

            
    /// Constructs HttpRequestMessage with given method, url and optional headers
    let private getRequestMessage (httpMethod: HttpMethod) (url: string) (urlSuffix: string)
                                  (headers: Map<string, string> option) (content: HttpContent option)
                                  : HttpRequestMessage =
        let requestMessage = new HttpRequestMessage(httpMethod, $"{url}/{urlSuffix}")
        
        match content with
        | Some c -> requestMessage.Content <- c
        | _      -> ()
        match headers with
        | Some h -> addRequestHeaders h requestMessage.Headers
        | _      -> ()
        
        requestMessage
        
    /// Performs http GET request
    let get (urlSuffix: string) (headers: Map<string, string> option)
            (connection: PostgrestConnection): Async<Result<HttpResponseMessage, PostgrestError>> =
        let requestMessage = getRequestMessage HttpMethod.Get connection.Url urlSuffix headers None

        executeHttpRequest requestMessage connection
        
    /// Performs http DELETE request
    let delete (urlSuffix: string) (headers: Map<string, string> option) (content: HttpContent option)
               (connection: PostgrestConnection): Async<Result<HttpResponseMessage, PostgrestError>> =
        let requestMessage = getRequestMessage HttpMethod.Delete connection.Url urlSuffix headers content
        
        executeHttpRequest requestMessage connection 
    
    /// Performs http POST request
    let post (urlSuffix: string) (headers: Map<string, string> option) (content: HttpContent)
             (connection: PostgrestConnection): Async<Result<HttpResponseMessage, PostgrestError>> =
        let requestMessage = getRequestMessage HttpMethod.Post connection.Url urlSuffix headers (Some content)
        
        executeHttpRequest requestMessage connection 
            
    /// Performs http PATCH request
    let patch (urlSuffix: string) (headers: Map<string, string> option) (content: HttpContent)
              (connection: PostgrestConnection): Async<Result<HttpResponseMessage, PostgrestError>> =
        let requestMessage = getRequestMessage HttpMethod.Patch connection.Url urlSuffix headers (Some content)
        
        executeHttpRequest requestMessage connection