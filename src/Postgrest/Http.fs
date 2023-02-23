namespace Postgrest.Http

open System.Net
open System.Net.Http
open FSharp.Json
open Postgrest.Connection
open Postgrest.Common

[<AutoOpen>]
module Http =
    type PostgrestError = {
        message: string
        statusCode: HttpStatusCode
    }
    
    let private getResponseBody (responseMessage: HttpResponseMessage): string = 
        responseMessage.Content.ReadAsStringAsync()
        |> Async.AwaitTask
        |> Async.RunSynchronously
            
    let deserializeResponse<'T> (response: Result<HttpResponseMessage, PostgrestError>): Result<'T, PostgrestError> =        
        match response with
        | Ok r    ->
            printfn $"{r.RequestMessage}"
            Result.Ok (Json.deserialize<'T> (r |> getResponseBody))
        | Error e -> Result.Error e
        
    let deserializeEmptyResponse (response: Result<HttpResponseMessage, PostgrestError>): Result<unit, PostgrestError> =
        match response with
        | Ok _    -> Result.Ok ()
        | Error e -> Result.Error e
        
    let executeHttpRequest (headers: Map<string, string> option) (requestMessage: HttpRequestMessage)
                           (connection: PostgrestConnection): Result<HttpResponseMessage, PostgrestError> =
        try
            let httpClient = connection.HttpClient
            let result =
                task {
                    requestMessage.Headers |> addRequestHeaders connection.Headers
                    
                    match headers with
                    | Some h -> requestMessage.Headers |> addRequestHeaders h
                    | _      -> ()
                    
                    let response = httpClient.SendAsync(requestMessage)
                    return! response
                } |> Async.AwaitTask |> Async.RunSynchronously
            match result.StatusCode with
            | HttpStatusCode.OK -> Result.Ok result
            | statusCode        ->
                Result.Error { message    = result |> getResponseBody
                               statusCode = statusCode }
        with e ->
            Result.Error { message    = e.ToString()
                           statusCode = HttpStatusCode.BadRequest }
            
    let private getRequestMessage (httpMethod: HttpMethod) (url: string) (urlSuffix: string): HttpRequestMessage =
        new HttpRequestMessage(httpMethod, $"{url}/{urlSuffix}")
        
    let get (urlSuffix: string) (headers: Map<string, string> option)
            (connection: PostgrestConnection): Result<HttpResponseMessage, PostgrestError> =
        let requestMessage = getRequestMessage HttpMethod.Get connection.Url urlSuffix

        connection |> executeHttpRequest headers requestMessage
        
    let delete (urlSuffix: string) (headers: Map<string, string> option)
               (connection: PostgrestConnection): Result<HttpResponseMessage, PostgrestError> =
        let requestMessage = getRequestMessage HttpMethod.Delete connection.Url urlSuffix
        
        connection |> executeHttpRequest headers requestMessage 
    
    let post (urlSuffix: string) (headers: Map<string, string> option) (content: StringContent)
             (connection: PostgrestConnection): Result<HttpResponseMessage, PostgrestError> =
        let requestMessage = getRequestMessage HttpMethod.Post connection.Url urlSuffix
        requestMessage.Content <- content
        
        connection |> executeHttpRequest headers requestMessage 
            
    let patch (urlSuffix: string) (headers: Map<string, string> option) (content: StringContent)
              (connection: PostgrestConnection): Result<HttpResponseMessage, PostgrestError> =
        let requestMessage = getRequestMessage HttpMethod.Patch connection.Url urlSuffix
        requestMessage.Content <- content
        
        connection |> executeHttpRequest headers requestMessage 