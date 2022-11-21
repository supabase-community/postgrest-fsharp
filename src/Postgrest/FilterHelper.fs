namespace Postgrest

[<AutoOpen>]
module Filter =
    
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