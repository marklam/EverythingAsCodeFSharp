## Converting property-based tests to Hedgehog
I'd used [FsCheck](https://github.com/fscheck/FsCheck) and [Hedgehog](https://github.com/hedgehogqa/fsharp-hedgehog) in the past, and I'd found Hedgehog
to be more 'friendly', and produce better failing cases using shrinking, but FsCheck seemed faster. I'd started this project with FsCheck to see if I liked
it better now, but makng and using custom generators still wasn't to my liking, so I switched to Hedgehog.

I changed the package references
```cmd
dotnet paket remove FsCheck.Xunit --project WordValues.Tests
dotnet paket add Hedgehog --project WordValues.Tests
dotnet restore
```
The generator for word lists went from
```fsharp
type WordList =
    | WordList of (string*int) list with
    static member Generator =
        let genWord =
            Arb.generate<char>
            |> Gen.filter (Char.IsLetter)
            |> Gen.arrayOf
            |> Gen.map String

        genWord
        |> Gen.map (fun w -> w, (Calculate.wordValue w).Value)
        |> Gen.filter (fun (w, v) -> v > 0)
        |> Gen.listOf
        |> Gen.map WordList
        |> Arb.fromGen
```
to 
```fsharp
module Gen =
    let wordList =
        Gen.string (Range.linear 1 20) Gen.alpha
        |> Gen.map (fun w -> w, (Calculate.wordValue w).Value)
        |> Gen.list (Range.linear 0 100)
```
I changed the FsCheck.Xunit `Property` attributes to straight Xunit `Fact` attributes. 
Then the tests could be done using the `property` computation expression, and the checking was done with Unquote.
```diff
+module Gen =
+    let nonNullString =
+        Gen.string (Range.linear 0 100) (Gen.char Char.MinValue Char.MaxValue)
+
-[<Property>]
+[<Fact>]
-let ``Value of text is below maximum value`` (nnstr : NonNull<string>) =
+let ``Value of text is below maximum value`` () =
+    property {
-    let str = nnstr.Get
+        let! str = Gen.nonNullString
-    (Calculate.wordValue str).Value <= 26 * str.Length[<Fact>]
+        test <@ (Calculate.wordValue str).Value <= 26 * str.Length @>
+    } |> Property.check
```
This showed up a test failure, nicely described in the Test Explorer along with how to reproduce it:
```
    System.Exception : *** Failed! Falsifiable (after 1 test and 15 shrinks):
    "?"
    Xunit.Sdk.TrueException: 
    
    (Calculate.wordValue str).Value <= 26 * str.Length
    (Calculate.wordValue "?").Value <= 26 * "?".Length
    { Value = 106
      Warning = None }.Value <= 26 * 1
    106 <= 26
    false
    
    Expected: True
    Actual:   False
       at WordValues.Tests.TestCalculate.Value of text is below maximum value@71-1.Invoke(String _arg1) in C:\git\EverythingAsCodeFSharp\WordValues.Tests\TestCalculate.fs:line 71
       at Hedgehog.Property.prepend@115-1.Invoke(Unit _arg1)
    This failure can be reproduced by running:
    > Property.recheck (1 : Size) ({ Value = 1298872065959223496UL; Gamma = 772578873708680621UL }) <property>
```
I changed the `Property.check` to `Property.recheck (1 : Size) ({ Value = 1298872065959223496UL; Gamma = 772578873708680621UL })` and ran under the debugger, 
it seems that the wordValue function was happy to assign a value to the word '搴', which is not in the range 'A'-'Z' or 'a'-'z'.

A second failure occurred in the test that `Warning contains non-letters`, because the characters 'χ' and 'ḳ' were upper-cased to 'Χ' and 'Ḳ' in the warning message.

It was while chasing down these odd cases with non-Ascii characters that I realised something about these property-based tests.
These `Property.recheck` calls could be added to the tests in addition to the `Property.check` to ensure that a regression 
does not occur (assuming that nothing changes in the generators).This would help ensure that after a bug found by the test is fixed, it isn't accidentally re-introduced later.

I added this helper
```fsharp
module Property =
    let regressionTest size seed prop =
        Property.recheck size seed prop
        prop
```
which returns the property being tested, so that it can be piped into `Property.check` as normal.
```fsharp
[<Fact>]
let ``Value of text is same as value of lower case`` () =
    property {
        let! str = Gen.nonNullString
        test <@ (Calculate.wordValue str).Value = (Calculate.wordValue (str.ToLower())).Value @>
    }
    |> Property.regressionTest 91 { Value = 9535703340393401501UL; Gamma = 8182104926013755423UL }
    |> Property.check
```