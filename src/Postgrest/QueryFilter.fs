namespace Postgrest

open Postgrest.Common

[<AutoOpen>]
module QueryFilter =
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
        
        
    type IsFilterValue =
        | IsNull
        | IsTrue
        | IsFalse
        | IsUnknown
        
    // type IsFilter = IsFilter of Column * IsFilterValue
        
    type Pattern = string
    type LikeFilter = Column * Pattern
    type ILikeFilter = LikeFilter
        
    type OrderType =
        | Ascending
        | Descending
        
    type OrderNull =
        | NullFirst
        | NullLast
     
    type FtsQuery = string
    type Language = string
    type FullTextSearch =
        | Fts   of FtsQuery list * Language option
        | Plfts of FtsQuery list * Language option
        | Phfts of FtsQuery list * Language option
        | Wfts  of FtsQuery list * Language option
        | FtsNot of FullTextSearch
        
    let private parseFilterValue (filterValue: FilterValue): string =
        match filterValue with
        | String s -> s
        | Int    i -> i.ToString()
        | Double d -> d.ToString()
        | Float  f -> f.ToString()
        | Bool   b -> b.ToString()
    
    let rec private buildFilterString (filter: Filter): string =
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
    
    let private getQueryFilterStringValue (queryFilterString: string option): string =
        match queryFilterString with
        | Some fs -> fs
        | _       -> ""
    
    let private first (a, _, _) = a
    let private middle (_, b, _) = b
    let private third (_, _, c) = c 
    let private getOrderByString (orderBy: string * OrderType option * OrderNull option): string =
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
            
        $"{item}{orderType}{orderNull}"
    
    let filter (filter: Filter) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        let currentQueryFilterString = pfb.QueryFilterString |> getQueryFilterStringValue
        let filterString = $"{currentQueryFilterString}&" + (filter |> buildFilterString)
        
        { pfb with QueryFilterString = Some filterString }
        
    let in_ (filterIn: Column * 'a list) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        let key, items = filterIn
        let stringValues = items |> List.map (fun item -> item.ToString())
        let currentQueryFilterString = pfb.QueryFilterString |> getQueryFilterStringValue
        
        let filterString = $"{currentQueryFilterString}&{key}=in."
                           + "(" + (stringValues |> joinQueryParams) + ")"
                           
        { pfb with QueryFilterString = Some filterString }
    
    let getIsFilterValue (isFilter: IsFilterValue): string =
        match isFilter with
        | IsNull    -> "null"
        | IsTrue    -> "true"
        | IsFalse   -> "false"
        | IsUnknown -> "unknown"
        
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
    
    let internal joinFtsParams (ftsParams: string list): string =
        ftsParams |> List.reduce (fun acc item -> acc + "%20" + item)
        
    let parseFtsConfig (config: string option): string =
        match config with
        | Some v -> "(" + v + ")"
        | _      -> ""
        
    let rec buildFtsString (ftsParam: FullTextSearch): string = 
        match ftsParam with
            | Fts    (query, config) -> "fts"    + (config |> parseFtsConfig) + "." + (query |> joinFtsParams)
            | Plfts  (query, config) -> "plfts." + (config |> parseFtsConfig) + "." + (query |> joinFtsParams) 
            | Phfts  (query, config) -> "phfts." + (config |> parseFtsConfig) + "." + (query |> joinFtsParams)
            | Wfts   (query, config) -> "wfts."  + (config |> parseFtsConfig) + "." + (query |> joinFtsParams)
            | FtsNot fts1            -> "not."   + buildFtsString fts1
        
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
        
        { pfb with Query = { pfb.Query with Connection = { Headers = updatedHeaders ; Url = pfb.Query.Connection.Url } } }