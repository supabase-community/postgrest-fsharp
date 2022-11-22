namespace Postgrest

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
        
    let internal getQueryFilterStringValue (queryFilterString: string option): string =
        match queryFilterString with
        | Some fs -> fs
        | _       -> ""
        
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
            
        $"{item}{orderType}{orderNull}"
        
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
        | Fts   of FtsQuery list * Language option
        | Plfts of FtsQuery list * Language option
        | Phfts of FtsQuery list * Language option
        | Wfts  of FtsQuery list * Language option
        | FtsNot of FullTextSearch
        
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