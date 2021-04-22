**remember to put the ece type back in whichever branch it got deleted **

## Making the function do something
A 'Hello World' function is a trivial demo, so now I'll make it slightly less trivial.

Puzzles set by users in the real-world [geocaching](https://www.geocaching.com/play) game sometimes involve
converting answers in the form of a word into digits to make up co-ordinates of a hidden item. 

The method is simple enough - each letter in the textual answer gets assigned a value where A=1, B=2, ... Z=26
and then the values are summed.
### An implementation and tests
The word value implementation isn't particularly interesting, but I'll provide a naive implementation of
it and some tests.

I don't want to be spinning up hosting processes, or deploying & invoking the real web service to test the algorithm,
so I'll extract the implementation from the cloud function (so 'WordValues' and 'WordValues.Azure').

For tests, I'm using xUnit and [Swensen Unquote](https://github.com/SwensenSoftware/unquote). With Unquote you write the test condition in an F#
quotation, and if a test fails you get some explanation of what failed. For example, if there was a
bug in the test where I forget there are two Ls in HELLO - then the test
```fsharp
[<Fact>]
let ``Value of HELLO is correct`` () =
      test <@ Calculate.wordValue "HELLO" = 8 + 5 + 12 + 15 @>
```
would produce the output
```
  Message: 
    
    
    Calculate.wordValue "HELLO" = 8 + 5 + 12 + 15
    52 = 13 + 12 + 15
    52 = 25 + 15
    52 = 40
    false
    
    Expected: True
    Actual:   False
```

### Property-based tests
Supposing with that test fixed, I was happy that the implementation was complete.
```
    let wordValue (text : string) : int =
        text.ToUpper()
        |> Seq.sumBy (fun letter -> (int letter) - (int 'A') + 1)
```

But *just in case*, I'll create some property-based tests. Property-based testing describes some
properties of how the function should behave when given unknown inputs.

Some properties that we could test:
The value of some text is the same as the value of its all-upper-case version
The value of some text is the same as the value of its all-lower-case version
The value of some text should be the same as the value of the reversed of the text
The value should be at most 26 * the character count
```fsharp
[<Property>]
let ``Value of text is same as value of upper case`` (str : string) =
    Calculate.wordValue str = Calculate.wordValue (str.ToUpper())

[<Property>]
let ``Value of text is same as value of lower case`` (str : string) =
    Calculate.wordValue str = Calculate.wordValue (str.ToLower())

[<Property>]
let ``Value of text is same as value of reversed text`` (str : string) =
    Calculate.wordValue str = Calculate.wordValue (reverse str)

[<Property>]
let ``Value of text is below maximum value`` (str : string) =
    Calculate.wordValue str <= 26 * str.Length
```
And those tests failed instantly, because FsCheck supplied 'null' for the strings. 

Since we're in F# we can be fairly certain that's not going to be something we pass to the wordValue calculation.
By changing the parameter type in the `Property` test from `string` to `NonNull<string>` we can
remove the `null`s from the tests.

Next failure is
```
    FsCheck.Xunit.PropertyFailedException : 
    Falsifiable, after 11 tests (1 shrink) (StdGen (824747591, 296879486)):
    Original:
    NonNull "X]"
    Shrunk:
    NonNull "]"
```
So the test failed for "X]", and then FsCheck tried to find a smaller repro case - "]". 

After filtering out non-letters in the calculation, the tests pass. But I'll change the return value to 
be a value and a warning message, and add a test of the warnings too.

The property tests noticed that the warnings were different if the source text was reversed, so I 
made the warning report each ignored character once, in character-code order.

### Returning the calculation results from the Azure Function
I had to add some code to convert the result of the calculation (the value and any warnings) to json
using `System.Text.Json`'s JsonSerializer.

I also added some tests to the WordValues.Azure.Tests project to check the returned json.

I also had to read the word to evaluate from the query parameters of the HttpRequest. Of course the
first thing that happened was that I got `null`s  for the word where the parameter was missing from the URL.

I guess that serves me right for claiming we wouldn't see those in F#. To isolate them, I added 
```fsharp
module NameValueCollection =
    let tryFind (key : string) (nvc : NameValueCollection) =
        nvc.[key] |> Option.ofObj
```
to turn `null`s into Option.None

I also found that I could speed up the function hosting under the test by passing `--no-build` provided
I can find the build output folder for the function assembly, so I added some code to the tests to 'guess' that path.


