namespace Postgrest

open Postgrest.Common

[<AutoOpen>]
module PostgrestFilterBuilder =
    type PostgrestFilterBuilder = {
        Query            : Query
        QueryFilterString: string option
        QueryInString    : string option
        QueryIsString    : string option
        QueryOrderString : string option
        QueryLimitString : string option
        QueryOffsetString: string option
        QueryLikeString  : string option
        QueryILikeString : string option
        QueryFtsString   : string option
        Body             : RequestBody option
        RequestType      : FilterRequestType   
    }
        
    type Pattern = string
    type LikeFilter = Column * Pattern
    type ILikeFilter = LikeFilter
    
    let filter (filter: Filter) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        let currentQueryFilterString = pfb.QueryFilterString |> getQueryFilterStringValue
        let filterString = $"{currentQueryFilterString}&" + (filter |> buildFilterString)
        
        { pfb with QueryFilterString = Some filterString }
        
    let in' (filterIn: Column * 'a list) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        let key, items = filterIn
        
        match items.IsEmpty with
        | false ->
            let stringValues = items |> List.map (fun item -> item.ToString())
            let inString = $"&{key}=in." + "(" + (stringValues |> joinQueryParams) + ")"
                               
            { pfb with QueryInString = Some inString }
        | true ->
            pfb
        
    let is (isFilter: Column * IsFilterValue) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        let column, filter = isFilter
        let isFilterValueString = filter |> getIsFilterValue
        
        { pfb with QueryIsString = Some $"&{column}=is.{isFilterValueString}" }
    
    let like (likeFilter: LikeFilter) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        let column, pattern = likeFilter
        
        { pfb with QueryLikeString = Some $"&{column}=like.{pattern}" }
        
    let ilike (iLikeFilter: ILikeFilter) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        let column, pattern = iLikeFilter
        
        { pfb with QueryILikeString = Some $"&{column}=ilike.{pattern}" }
    
    let order (orderBy: (Column * OrderType option * OrderNull option) list)
              (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        let orderByItems = orderBy |> List.map getOrderByString
        let orderByString =
            match orderByItems.IsEmpty with
            | true -> ""
            | _    -> "&order=" + (orderByItems |> joinQueryParams)
        
        { pfb with QueryOrderString  = Some orderByString }
        
    let limit (items: int) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        { pfb with QueryLimitString = Some $"&limit={items}" }
        
    let offset (items: int) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        { pfb with QueryOffsetString = Some $"&offset={items}" }
        
    let fts (ftsParam: Column * FullTextSearch) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        let parsedParam = snd ftsParam |> buildFtsString
        let column = fst ftsParam
        
        { pfb with QueryFtsString = Some ("&" + column + "=" + parsedParam) }
        
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