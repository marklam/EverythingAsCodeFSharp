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
