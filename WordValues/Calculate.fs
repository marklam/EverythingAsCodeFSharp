namespace WordValues

module Calculate =
    let wordValue (text : string) : int =
        text.ToUpper()
        |> Seq.sumBy (fun letter -> (int letter) - (int 'A') + 1)
