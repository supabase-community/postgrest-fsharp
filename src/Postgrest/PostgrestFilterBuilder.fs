namespace Postgrest

open Postgrest.Common
open Postgrest.Http

/// Contains functions and types for for executing and filtering operations
[<AutoOpen>]
module PostgrestFilterBuilder =
    /// Represents pattern
    type Pattern = string
    /// Represents like filter
    type LikeFilter = Column * Pattern
    /// Represents ilike filter
    type ILikeFilter = Column * Pattern
    
    /// Executes given `PostgrestFilterBuilder` query and deserializes response
    let execute<'T> (pfb: PostgrestFilterBuilder): Async<Result<'T, PostgrestError>> = 
        async {
            let! response =
                match pfb.RequestType with
                | Select -> pfb |> executeSelect
                | Delete -> pfb |> executeDelete
                | Update -> pfb |> executeUpdate
                
            return deserializeResponse<'T> response
        }
    
    /// Adds filter to query
    let filter (filter: Filter) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        let currentQueryFilterString = ("", pfb.QueryFilterString) ||> Option.defaultValue
        let filterString = $"{currentQueryFilterString}&" + (buildFilterString filter)
        
        { pfb with QueryFilterString = Some filterString }
        
    /// Adds in filter to query
    let in' (filterIn: Column * 'a list) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        let key, items = filterIn
        
        match items.IsEmpty with
        | false ->
            let stringValues = items |> List.map (fun item -> item.ToString())
            let inString = $"&{key}=in." + "(" + (joinQueryParams stringValues) + ")"
                               
            { pfb with QueryInString = Some inString }
        | true -> pfb
        
    /// Adds is filter to query
    let is (isFilter: Column * IsFilterValue) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        let column, filter = isFilter
        let isFilterValueString = getIsFilterValue filter
        
        { pfb with QueryIsString = Some $"&{column}=is.{isFilterValueString}" }
    
    /// Adds like pattern filter to query
    let like (likeFilter: LikeFilter) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        let column, pattern = likeFilter
        
        { pfb with QueryLikeString = Some $"&{column}=like.{pattern}" }
        
    /// Adds ilike pattern filter to query
    let ilike (iLikeFilter: ILikeFilter) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        let column, pattern = iLikeFilter
        
        { pfb with QueryILikeString = Some $"&{column}=ilike.{pattern}" }
    
    /// Adds ordering to query
    let order (orderBy: (Column * OrderType option * OrderNull option) list)
              (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        match orderBy.IsEmpty with
        | true -> pfb
        | _    ->
            let orderByItems = orderBy |> List.map getOrderByString
            
            match orderByItems.IsEmpty with
            | true -> pfb
            | _    ->
                let orderByString = "&order=" + (joinQueryParams orderByItems)
                { pfb with QueryOrderString  = Some orderByString }
        
    /// Adds limit to query
    let limit (items: int) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        { pfb with QueryLimitString = Some $"&limit={items}" }
        
    /// Adds offset to query
    let offset (items: int) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        { pfb with QueryOffsetString = Some $"&offset={items}" }
        
    /// Adds full text search to query
    let fts (ftsParam: Column * FullTextSearch) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        let parsedParam = snd ftsParam |> buildFtsString
        let column = fst ftsParam
        
        match parsedParam.Length = 0 && column.Length = 0 with
        | true -> pfb
        | _    -> { pfb with QueryFtsString = Some $"&{column}={parsedParam}" }
        
    /// Updates header to expect only one result to be returned
    let one (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        let updatedHeaders =
            match pfb.Query.Connection.Headers.TryFind "Accept" with
            | Some header ->
                let headers = header.Split "/"
                match headers.Length = 2 with
                | true ->
                    pfb.Query.Connection.Headers.Add("Accept", $"{headers[0]}/vnd.pgrst.object+{headers[1]}")
                | false ->
                    pfb.Query.Connection.Headers.Add("Accept", $"{headers[0]}/vnd.pgrst.object")
            | None        -> pfb.Query.Connection.Headers.Add("Accept", "application/vnd.pgrst.object")
        
        { pfb with Query = { pfb.Query with Connection = { Headers = updatedHeaders
                                                           Url = pfb.Query.Connection.Url
                                                           HttpClient = pfb.Query.Connection.HttpClient } } }