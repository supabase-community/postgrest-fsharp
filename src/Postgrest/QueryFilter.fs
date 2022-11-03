namespace Postgrest.QueryFilter

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
        | EQ  of  string * FilterValue
        | GT  of  string * FilterValue
        | GTE of  string * FilterValue
        | LT  of  string * FilterValue
        | LTE of  string * FilterValue
        | NEQ of  string * FilterValue
        | NOT of  Filter
        | OR  of  Filter * Filter
        | AND of  Filter * Filter
        
    type OrderType =
        | ASC
        | DSC
        
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
        | EQ  (field, value) -> $"{field}=eq." + parseFilterValue value
        | GT  (field, value) -> $"{field}=gt." + parseFilterValue value
        | GTE (field, value) -> $"{field}=gte." + parseFilterValue value
        | LT  (field, value) -> $"{field}=lt." + parseFilterValue value
        | LTE (field, value) -> $"{field}=lte." + parseFilterValue value
        | NEQ (field, value) -> $"{field}=neq." + parseFilterValue value
        | NOT f              -> "not." + buildFilterString f
        | OR  (f1, f2)       -> "or=(" + buildFilterString f1 + "," + buildFilterString f2 + ")"
        | AND (f1, f2)       -> "and=(" + buildFilterString f1 + "," + buildFilterString f2 + ")"
        
    let private concatQueryFilterString (queryFilterString: string option): string =
        match queryFilterString with
        | Some fs -> fs
        | _       -> ""
        
    let private getOrderByString (orderBy: string * OrderType option * OrderNull option): string =
        let first (a, _, _) = a
        let middle (_, b, _) = b
        let third (_, _, c) = c
        
        let item = orderBy |> first
        
        let orderType =
           match orderBy |> middle with
           | Some s ->
                match s with
                | ASC -> ".asc"
                | DSC -> ".desc"
           | None   -> ""
           
        let orderNull =
            match orderBy |> third with
            | Some s ->
                match s with
                | NullFirst -> ".nullsfirst"
                | NullLast  -> ".nullslast"
            | None   -> ""
            
        $"{item}{orderType}{orderNull}"
    
    let filter (filter: Filter) (request: GetRequest): GetRequest =
        let currentQueryFilterString = request.QueryFilterString |> concatQueryFilterString
        let filterString = $"{currentQueryFilterString}&" + (filter |> buildFilterString)
        
        { request with QueryFilterString = Some filterString }
        
    let in_ (filterIn: string * 'a list) (request: GetRequest): GetRequest =
        let stringValues = (snd filterIn) |> List.map (fun item -> item.ToString())
        let currentQueryFilterString = request.QueryFilterString |> concatQueryFilterString
        
        let filterString = $"{currentQueryFilterString}&{fst filterIn}=in." + "(" +
                           (stringValues |> List.reduce(fun acc item -> $"{acc},{item}")) + ")"
        { request with QueryFilterString = Some filterString }
    
    let order (orderBy: (string * OrderType option * OrderNull option) list)
              (request: GetRequest): GetRequest =
        let orderByItems = orderBy |> List.map getOrderByString
        let orderByString =
            match orderByItems.IsEmpty with
            | true -> ""
            | _    -> "&order=" + (orderByItems |> List.reduce(fun acc item -> $"{acc},{item}"))
        
        { request with QueryOrderString  = Some orderByString }
        
    let limit (items: int) (request: GetRequest): GetRequest =
        { request with QueryLimitString = Some $"&limit={items}" }
        
    let offset (items: int) (request: GetRequest): GetRequest =
        { request with QueryOffsetString = Some $"&offset={items}" }