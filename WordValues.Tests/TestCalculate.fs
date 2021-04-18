module WordValues.Tests.TestCalculate

open Xunit
open Swensen.Unquote

open WordValues

[<Fact>]
let ``Value of 'HELLO' is correct`` () =
      test <@ Calculate.wordValue "HELLO" = 8 + 5 + 12 + 12 + 15 @>

[<Fact>]
let ``Value of 'hello' is correct`` () =
      test <@ Calculate.wordValue "hello" = 8 + 5 + 12 + 12 + 15 @>
