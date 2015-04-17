namespace FirstLine

module List =
    
    let rec skip n (lst : 'a list) =
        match lst with
        | _::tail -> if n <= 1 then tail else tail |> skip(n-1)
        | _ -> []

    let rec take n (lst : 'a list) =
        match lst with
        | head::tail -> if n > 0 then head :: (tail |> take(n-1)) else []
        | _ -> []

    let rec fragment n (lst : 'a list) =
        match lst with
        | _::_ -> (lst |> take(n)):: fragment n (lst |> skip(n))
        | _ -> []

