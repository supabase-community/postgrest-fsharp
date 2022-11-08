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
        | OpEqual            of  string * FilterValue
        | OpGreaterThan      of  string * FilterValue
        | OpGreaterThanEqual of  string * FilterValue
        | OpLessThan         of  string * FilterValue
        | OpLessThanEqual    of  string * FilterValue
        | OpNotEqual         of  string * FilterValue
        | OpNot              of  Filter
        | OpOr               of  Filter * Filter
        | OpAnd              of  Filter * Filter
        
    type OrderType =
        | Ascending
        | Descending
        
    type OrderNull =
        | NullFirst
        | NullLast
        
    let private parseFilterValue (filterValue: FilterValue): string =
        match filterValue with
        | String s -> s
        | Int    i -> i.ToString()
        | Double d -> d.ToString()
        | Float  f -> f.ToString()
        | Bool   b -> b.ToString()
    
    let rec private buildFilterString (filter: Filter): string = 
        match filter with
        | OpEqual  (field, value) -> $"{field}=eq." + parseFilterValue value
        | OpGreaterThan  (field, value) -> $"{field}=gt." + parseFilterValue value
        | OpGreaterThanEqual (field, value) -> $"{field}=gte." + parseFilterValue value
        | OpLessThan  (field, value) -> $"{field}=lt." + parseFilterValue value
        | OpLessThanEqual (field, value) -> $"{field}=lte." + parseFilterValue value
        | OpNotEqual (field, value) -> $"{field}=neq." + parseFilterValue value
        | OpNot f              -> "not." + buildFilterString f
        | OpOr  (f1, f2)       -> "or=(" + buildFilterString f1 + "," + buildFilterString f2 + ")"
        | OpAnd (f1, f2)       -> "and=(" + buildFilterString f1 + "," + buildFilterString f2 + ")"
    
    let private concatQueryFilterString (queryFilterString: string option): string =
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
        let currentQueryFilterString = pfb.QueryFilterString |> concatQueryFilterString
        let filterString = $"{currentQueryFilterString}&" + (filter |> buildFilterString)
        
        { pfb with QueryFilterString = Some filterString }
        
    let in_ (filterIn: string * 'a list) (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        let key, items = filterIn
        let stringValues = items |> List.map (fun item -> item.ToString())
        let currentQueryFilterString = pfb.QueryFilterString |> concatQueryFilterString
        
        let filterString = $"{currentQueryFilterString}&{key}=in."
                           + "(" + (stringValues |> joinQueryParams) + ")"
                           
        { pfb with QueryFilterString = Some filterString }
    
    let order (orderBy: (string * OrderType option * OrderNull option) list)
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
        
    let one (pfb: PostgrestFilterBuilder): PostgrestFilterBuilder =
        let updatedHeaders =
            match pfb.Query.Connection.Headers.TryFind "Accept" with
            | Some header ->
                let splitedHeader = header.Split "/"
                match splitedHeader.Length = 2  with
                | true ->
                    pfb.Query.Connection.Headers.Add("Accept", $"{splitedHeader[0]}/vnd.pgrst.object+{splitedHeader[1]}")
                | false ->
                    pfb.Query.Connection.Headers.Add("Accept", $"{splitedHeader[0]}/vnd.pgrst.object")
            | None        -> pfb.Query.Connection.Headers.Add("Accept", "application/vnd.pgrst.object")
        
        { pfb with Query = { pfb.Query with Connection = { Headers = updatedHeaders ; Url = pfb.Query.Connection.Url } } }