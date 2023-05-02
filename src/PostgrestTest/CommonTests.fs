module CommonTests

open FsUnit.Xunit
open Xunit
open Postgrest
open Postgrest.Common

[<Collection("parseColumns")>]
module ParseColumnsTests =
    [<Fact>]
    let ``returns * if columns is All`` () =
        // Arrange
        let expectedResult = "*"
        let columns = All

        // Act
        let result = Common.parseColumns columns 

        // Assert
        result |> should equal expectedResult
        
    [<Fact>]
    let ``returns * if columns is Cols with empty list`` () =
        // Arrange
        let expectedResult = "*"
        let columns = Columns []

        // Act
        let result = Common.parseColumns columns 

        // Assert
        result |> should equal expectedResult
        
    [<Fact>]
    let ``returns given columns if columns is Cols with non empty list`` () =
        // Arrange
        let expectedResult = "col-1,col-2"
        let columns = Columns [ "col-1" ; "col-2"]

        // Act
        let result = Common.parseColumns columns 

        // Assert
        result |> should equal expectedResult