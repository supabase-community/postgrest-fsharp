namespace Postgrest

open System.Net.Http
open Postgrest.Common
open Postgrest.Http

/// Contains helper functions and types for `PostgrestFilterBuilder.fs` module
module PostgrestFilterBuilderHelper =
    /// Constructs url suffix from all `Query` params
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
    
    /// Executes select query
    let internal executeSelect<'T> (pfb: PostgrestFilterBuilder): Async<Result<HttpResponseMessage, PostgrestError>> =
        let urlSuffix = getUrlSuffixFromPostgresFilterBuilder pfb
        
        get urlSuffix None pfb.Query.Connection
        
    /// Executes delete query
    let internal executeDelete (pfb: PostgrestFilterBuilder): Async<Result<HttpResponseMessage, PostgrestError>> =
        let urlSuffix = getUrlSuffixFromPostgresFilterBuilder pfb
            
        delete urlSuffix (Some (Map [ "Prefer" , "return=representation" ] )) None pfb.Query.Connection
    
    /// Executes update query
    let internal executeUpdate (pfb: PostgrestFilterBuilder): Async<Result<HttpResponseMessage, PostgrestError>> =
        match pfb.Body with
        | Some body ->
            let urlSuffix = getUrlSuffixFromPostgresFilterBuilder pfb
            let content = getStringContent body
            
            patch urlSuffix (Some (Map [ "Prefer" , "return=representation" ] )) content pfb.Query.Connection
        | _ -> async { return Error { message = "Missing request body" ; statusCode = None } }

/// Contains helper functions and types for filtering operations
[<AutoOpen>]
module FilterHelpers =
    /// Represents possible filter values
    type FilterValue =
        | String of string
        | Int    of int
        | Double of double
        | Float  of float
        | Bool   of bool
    
    /// Represents filter item (how and what to filter)
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

    /// Converts `FilterValue` to it's string representation
    let private parseFilterValue (filterValue: FilterValue): string =
        match filterValue with
        | String s -> s
        | Int    i -> i.ToString()
        | Double d -> d.ToString()
        | Float  f -> f.ToString()
        | Bool   b -> b.ToString().ToLower()
    
    /// Builds result filter string
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
        
/// Contains helper functions and types for ordering operations
[<AutoOpen>]
module OrderByHelpers =
    /// Represents ordering options
    type OrderType =
        | Ascending
        | Descending
        
    /// Represents null ordering options
    type OrderNull =
        | NullFirst
        | NullLast
        
    /// Returns first item in triple
    let private first (a, _, _) = a
    /// Returns second item in triple
    let private middle (_, b, _) = b
    /// Returns third item in triple
    let private third (_, _, c) = c 
    
    /// Returns order by string representation
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
        
/// Contains is filter helper functions and types
[<AutoOpen>]
module IsFilterHelpers =
     /// Represents is filter possible values
     type IsFilterValue =
        | IsNull
        | IsTrue
        | IsFalse
        | IsUnknown
        
     /// Returns `IsFilterValue` string representation
     let getIsFilterValue (isFilter: IsFilterValue): string =
        match isFilter with
        | IsNull    -> "null"
        | IsTrue    -> "true"
        | IsFalse   -> "false"
        | IsUnknown -> "unknown"
        
/// Contains full text search helper functions and types
[<AutoOpen>]
module FtsHelpers =
    /// Represents text of full text search
    type FtsQuery = string
    
    /// Represents full text search language
    type Language = string
    
    /// Represents full text search type
    type FullTextSearch =
        | Fts    of FtsQuery list * Language option
        | Plfts  of FtsQuery list * Language option
        | Phfts  of FtsQuery list * Language option
        | Wfts   of FtsQuery list * Language option
        | FtsNot of FullTextSearch
    
    /// Joins given full text search params to string    
    let private joinFtsParams (ftsParams: string list): string =
        match ftsParams.IsEmpty with
        | true -> ""
        | _    -> ftsParams |> List.reduce (fun acc item -> acc + "%20" + item)
        
    /// Parses optional full text search config (language)
    let private parseFtsConfig (config: string option): string =
        match config with
        | Some v -> "(" + v + ")"
        | _      -> ""
        
    /// Constructs full text search item string representation 
    let private buildFtsStringInner (prefix: string) (config: Language Option) (query: FtsQuery list): string =
        $"{prefix}{parseFtsConfig config}.{joinFtsParams query}"
    
    /// Constructs full text search query string representation 
    let rec internal buildFtsString (fts: FullTextSearch): string = 
        match fts with
            | Fts    (query, config) -> buildFtsStringInner "fts" config query
            | Plfts  (query, config) -> buildFtsStringInner "plfts" config query 
            | Phfts  (query, config) -> buildFtsStringInner "phfts" config query
            | Wfts   (query, config) -> buildFtsStringInner "wfts" config query
            | FtsNot ftsNot          -> $"not.{buildFtsString ftsNot}"