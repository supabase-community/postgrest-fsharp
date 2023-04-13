namespace Postgrest

open System.Net
open System.Net.Http
open System.Text
open Postgrest.Http

[<AutoOpen>]
module PostgrestFilterBuilderHelper =
    let getUrlSuffixFromPostgresFilterBuilder (pfb: PostgrestFilterBuilder): string =
        let query = pfb.Query
        
        let queryFilterString = parseOptionalQueryString pfb.QueryFilterString
        let queryInString     = parseOptionalQueryString pfb.QueryInString
        let queryIsString     = parseOptionalQueryString pfb.QueryIsString
        let queryOrderString  = parseOptionalQueryString pfb.QueryOrderString
        let queryLimitString  = parseOptionalQueryString pfb.QueryLimitString
        let queryOffsetString = parseOptionalQueryString pfb.QueryOffsetString
        let queryLikeString   = parseOptionalQueryString pfb.QueryLikeString
        let queryILikeString  = parseOptionalQueryString pfb.QueryILikeString
        let queryFtsString    = parseOptionalQueryString pfb.QueryFtsString
            
        let urlSuffix =
            query.Table + query.QueryString + queryFilterString
            + queryInString + queryIsString + queryOrderString + queryLimitString
            + queryOffsetString + queryLikeString + queryILikeString + queryFtsString 
        
        urlSuffix
    
    let internal executeSelect<'T> (pfb: PostgrestFilterBuilder): Result<HttpResponseMessage, PostgrestError> =
        let urlSuffix = pfb |> getUrlSuffixFromPostgresFilterBuilder
        
        pfb.Query.Connection |> get urlSuffix None
        
    let internal executeDelete (pfb: PostgrestFilterBuilder): Result<HttpResponseMessage, PostgrestError> =
        let urlSuffix = pfb |> getUrlSuffixFromPostgresFilterBuilder
            
        pfb.Query.Connection |> delete urlSuffix (Some (Map [ "Prefer" , "return=representation" ] )) None
    
    let internal executeUpdate (pfb: PostgrestFilterBuilder): Result<HttpResponseMessage, PostgrestError> =
        match pfb.Body with
        | Some body ->
            let urlSuffix = pfb |> getUrlSuffixFromPostgresFilterBuilder
            let content = new StringContent(body, Encoding.UTF8, "application/json")
            
            pfb.Query.Connection |> patch urlSuffix (Some (Map [ "Prefer" , "return=representation" ] )) content
        | _ -> Error { message = "Missing request body" ; statusCode = None }

[<AutoOpen>]
module FilterHelpers =
    type FilterValue =
        | String of string
        | Int    of int
        | Double of double
        | Float  of float
        | Bool   of bool
    
    type Filter =
        | OpEqual            of  Column * FilterValue
        | OpGreaterThan      of  Column * FilterValue
        | OpGreaterThanEqual of  Column * FilterValue
        | OpLessThan         of  Column * FilterValue
        | OpLessThanEqual    of  Column * FilterValue
        | OpNotEqual         of  Column * FilterValue
        | OpNot              of  Filter
        | OpOr               of  Filter * Filter
        | OpAnd              of  Filter * Filter

    let private parseFilterValue (filterValue: FilterValue): string =
        match filterValue with
        | String s -> s
        | Int    i -> i.ToString()
        | Double d -> d.ToString()
        | Float  f -> f.ToString()
        | Bool   b -> b.ToString().ToLower()
    
    let rec internal buildFilterString (filter: Filter): string =
        match filter with
        | OpEqual            (field, value) -> $"{field}=eq."  + parseFilterValue value
        | OpGreaterThan      (field, value) -> $"{field}=gt."  + parseFilterValue value
        | OpGreaterThanEqual (field, value) -> $"{field}=gte." + parseFilterValue value
        | OpLessThan         (field, value) -> $"{field}=lt."  + parseFilterValue value
        | OpLessThanEqual    (field, value) -> $"{field}=lte." + parseFilterValue value
        | OpNotEqual         (field, value) -> $"{field}=neq." + parseFilterValue value
        | OpNot              f              -> "not."  + buildFilterString f
        | OpOr               (f1, f2)       -> "or=("  + buildFilterString f1 + "," + buildFilterString f2 + ")"
        | OpAnd              (f1, f2)       -> "and=(" + buildFilterString f1 + "," + buildFilterString f2 + ")"
        
[<AutoOpen>]
module OrderByHelpers =
    type OrderType =
        | Ascending
        | Descending
        
    type OrderNull =
        | NullFirst
        | NullLast
        
    let private first (a, _, _) = a
    let private middle (_, b, _) = b
    let private third (_, _, c) = c 
    
    let internal getOrderByString (orderBy: string * OrderType option * OrderNull option): string =
        let item = orderBy |> first
        
        let orderType =
           match orderBy |> middle with
           | Some s ->
                match s with
                | Ascending  -> ".asc"
                | Descending -> ".desc"
           | None   -> ""
           
        let orderNull =
            match orderBy |> third with
            | Some s ->
                match s with
                | NullFirst -> ".nullsfirst"
                | NullLast  -> ".nullslast"
            | None   -> ""
            
        let orderByString = $"{item}{orderType}{orderNull}"
        orderByString
        
[<AutoOpen>]
module IsFilterHelpers =
     type IsFilterValue =
        | IsNull
        | IsTrue
        | IsFalse
        | IsUnknown
        
     let getIsFilterValue (isFilter: IsFilterValue): string =
        match isFilter with
        | IsNull    -> "null"
        | IsTrue    -> "true"
        | IsFalse   -> "false"
        | IsUnknown -> "unknown"
        
[<AutoOpen>]
module FtsHelpers =
    type FtsQuery = string
    type Language = string
    type FullTextSearch =
        | Fts    of FtsQuery list * Language option
        | Plfts  of FtsQuery list * Language option
        | Phfts  of FtsQuery list * Language option
        | Wfts   of FtsQuery list * Language option
        | FtsNot of FullTextSearch
        
    let private joinFtsParams (ftsParams: string list): string =
        match ftsParams.IsEmpty with
        | true -> ""
        | _    -> ftsParams |> List.reduce (fun acc item -> acc + "%20" + item)
        
    let private parseFtsConfig (config: string option): string =
        match config with
        | Some v -> "(" + v + ")"
        | _      -> ""
        
    let private buildFtsStringInner (prefix: string) (config: Language Option) (query: FtsQuery list): string =
        $"{prefix}{parseFtsConfig config}.{joinFtsParams query}"
    
    let rec internal buildFtsString (fts: FullTextSearch): string = 
        match fts with
            | Fts    (query, config) -> buildFtsStringInner "fts" config query
            | Plfts  (query, config) -> buildFtsStringInner "plfts" config query 
            | Phfts  (query, config) -> buildFtsStringInner "phfts" config query
            | Wfts   (query, config) -> buildFtsStringInner "wfts" config query
            | FtsNot ftsNot          -> $"not.{buildFtsString ftsNot}"