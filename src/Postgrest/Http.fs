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
        statusCode: HttpStatusCode option
    }
    
    let private getResponseBody (responseMessage: HttpResponseMessage): string = 
        responseMessage.Content.ReadAsStringAsync()
        |> Async.AwaitTask
        |> Async.RunSynchronously
            
    let deserializeResponse<'T> (response: Result<HttpResponseMessage, PostgrestError>): Result<'T, PostgrestError> =        
        try
            match response with
            | Ok r    ->
                printfn $"{r.RequestMessage}"
                Result.Ok (Json.deserialize<'T> (r |> getResponseBody))
            | Error e -> Result.Error e
        with
            | :? System.NullReferenceException as ex ->
                Error { message = ex.Message ; statusCode = None }
            | _ ->
                Error { message = "Unexpected error" ; statusCode = None }
        
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
                    addRequestHeaders connection.Headers requestMessage.Headers
                    
                    match headers with
                    | Some h -> addRequestHeaders h requestMessage.Headers
                    | _      -> ()
                    
                    let response = httpClient.SendAsync(requestMessage)
                    return! response
                } |> Async.AwaitTask |> Async.RunSynchronously
            match result.StatusCode with
            | HttpStatusCode.OK -> Result.Ok result
            | statusCode        ->
                Result.Error { message    = getResponseBody result
                               statusCode = Some statusCode }
        with e ->
            Result.Error { message    = e.ToString()
                           statusCode = None }
            
    let private getRequestMessage (httpMethod: HttpMethod) (url: string) (urlSuffix: string): HttpRequestMessage =
        new HttpRequestMessage(httpMethod, $"{url}/{urlSuffix}")
        
    let get (urlSuffix: string) (headers: Map<string, string> option)
            (connection: PostgrestConnection): Result<HttpResponseMessage, PostgrestError> =
        let requestMessage = getRequestMessage HttpMethod.Get connection.Url urlSuffix

        executeHttpRequest headers requestMessage connection
        
    let delete (urlSuffix: string) (headers: Map<string, string> option) (content: HttpContent option)
               (connection: PostgrestConnection): Result<HttpResponseMessage, PostgrestError> =
        let requestMessage = getRequestMessage HttpMethod.Delete connection.Url urlSuffix
        match content with
        | Some c -> requestMessage.Content <- c
        | _      -> ()
        
        executeHttpRequest headers requestMessage connection 
    
    let post (urlSuffix: string) (headers: Map<string, string> option) (content: StringContent)
             (connection: PostgrestConnection): Result<HttpResponseMessage, PostgrestError> =
        let requestMessage = getRequestMessage HttpMethod.Post connection.Url urlSuffix
        requestMessage.Content <- content
        
        executeHttpRequest headers requestMessage connection 
            
    let patch (urlSuffix: string) (headers: Map<string, string> option) (content: StringContent)
              (connection: PostgrestConnection): Result<HttpResponseMessage, PostgrestError> =
        let requestMessage = getRequestMessage HttpMethod.Patch connection.Url urlSuffix
        requestMessage.Content <- content
        
        executeHttpRequest headers requestMessage connection 