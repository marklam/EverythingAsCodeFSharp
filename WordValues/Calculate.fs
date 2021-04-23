namespace WordValues

open System

type WordValue = { Value : int; Warning : string option }

module Calculate =
    let wordValue (text : string) : WordValue =
        let (letters, nonLetters) =
            text.ToUpper()
            |> List.ofSeq
            |> List.partition (Char.IsLetter)

        let value =
            letters
            |> List.sumBy (fun letter -> (int letter) - (int 'A') + 1)

        let warning =
            if List.isEmpty nonLetters then
                None
            else
                nonLetters
                |> List.distinct
                |> List.sort
                |> List.map (sprintf "'%c'")
                |> String.concat ","
                |> sprintf "Ignored %s"
                |> Some

        {
            Value   = value
            Warning = warning
        }
