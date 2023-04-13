namespace Postgrest

open Postgrest.Common
open Postgrest.Http

[<AutoOpen>]
module PostgrestFilterBuilder =
    type Pattern = string
    type LikeFilter = Column * Pattern
    type ILikeFilter = LikeFilter
    
    let execute<'T> (pfb: PostgrestFilterBuilder): Result<'T, PostgrestError> = 
        let response =
            match pfb.RequestType with
            | Select -> pfb |> executeSelect
            | Delete -> pfb |> executeDelete
            | Update -> pfb |> executeUpdate
            
        deserializeResponse<'T> response
    
    let filter (filter: Filter) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        let currentQueryFilterString = ("", pfb.QueryFilterString) ||> Option.defaultValue
        let filterString = $"{currentQueryFilterString}&" + (buildFilterString filter)
        
        { pfb with QueryFilterString = Some filterString }
        
    let in' (filterIn: Column * 'a list) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        let key, items = filterIn
        
        match items.IsEmpty with
        | false ->
            let stringValues = items |> List.map (fun item -> item.ToString())
            let inString = $"&{key}=in." + "(" + (joinQueryParams stringValues) + ")"
                               
            { pfb with QueryInString = Some inString }
        | true ->
            pfb
        
    let is (isFilter: Column * IsFilterValue) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        let column, filter = isFilter
        let isFilterValueString = getIsFilterValue filter
        
        { pfb with QueryIsString = Some $"&{column}=is.{isFilterValueString}" }
    
    let like (likeFilter: LikeFilter) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        let column, pattern = likeFilter
        
        { pfb with QueryLikeString = Some $"&{column}=like.{pattern}" }
        
    let ilike (iLikeFilter: ILikeFilter) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        let column, pattern = iLikeFilter
        
        { pfb with QueryILikeString = Some $"&{column}=ilike.{pattern}" }
    
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
        
    let limit (items: int) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        { pfb with QueryLimitString = Some $"&limit={items}" }
        
    let offset (items: int) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        { pfb with QueryOffsetString = Some $"&offset={items}" }
        
    let fts (ftsParam: Column * FullTextSearch) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        let parsedParam = snd ftsParam |> buildFtsString
        let column = fst ftsParam
        
        match parsedParam.Length = 0 && column.Length = 0 with
        | true -> pfb
        | _    -> { pfb with QueryFtsString = Some $"&{column}={parsedParam}" }
        
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