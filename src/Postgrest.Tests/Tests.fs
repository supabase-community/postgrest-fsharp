module Tests

open Xunit
open FsUnit.Xunit

[<Fact>]
let ``My test`` () =
    Assert.True(true)
    
[<Fact>]
let ``FSUnitTes`` () =
    1 |> should equal 1
