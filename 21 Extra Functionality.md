## Extra Functionality
Mostly just as a way of introducing some extra features to the project, I added a function to the WordValues assembly which will take a list of words with values,
and a desired 'total word value', and find sets of those words with the desired total value.

It's pretty brute-force, and not necessarily very efficient, but that gives scope for improvement. I defined a recursive function `fit` that takes the words used so far,
the unused words, and the remaining total to make up. It stops when the remaining total is 0, or there are no words left to try. It recurses both using the topmost word
and without using that word.

```fsharp
    let wordsFromValue (logger : ILogger) (wordValues : (string*int) list) (value : int) : string list list =
        let rec fit acc ws t =
            match ws, t with
            | _          , 0 -> [acc] // That's a solution
            | []         , _ -> []    // No more words to try
            | (w,v)::rest, _ ->
                (if (t < v) then [] else fit (w::acc) ws (t - v)) // Use w and fit the remainder
                @ (fit acc rest t)                                // Also try without using w

        Log.info logger (LogEvent.Create("wordsFromValue seeking {value}", value))

        let result =
            fit [] wordValues value
            |> List.filter (not << List.isEmpty)

        Log.info logger (LogEvent.Create("wordsFromValue got {count} results", result.Length))

        result
```

I also added some tests, including a property-based test which needed a custom generator for the wordValues list. I found the FsCheck way of doing this quite
'fiddly', and might try again using [Hedgehog](https://github.com/hedgehogqa/fsharp-hedgehog) instead.
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

[<Property( Arbitrary=[| typeof<WordList> |] )>]
let ``wordsFromValue matches wordValue`` (WordList wordList) (PositiveInt target) =
    Calculate.wordsFromValue wordList target
    |> List.forall (fun words -> (Calculate.wordValue (String.concat " " words)).Value = target)
```

The `WordList` discriminated union can be deconstructed in the parameter declaration on `wordsFromValue matches wordValue`. 

It has a static member function which generates words (sequences of letters), calculates their value, and removes any zeros (from empty string etc). 

FsCheck will use that generator due to the `Arbitrary=[| typeof<WordList> |]` property on the `Property` attribute.