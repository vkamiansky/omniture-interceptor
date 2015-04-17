namespace FirstLine
open System
open System.Linq
open log4net

module interceptor =

    let writeIfNotEmpty str = 
        if str |> (String.IsNullOrEmpty >> not) then printf "%s\r\n" str

    let rec skip n (lst : 'a list) =
        match lst with
        | _::tail -> if n <= 1 then tail else tail |> skip(n-1)
        | _ -> []

    let rec take n (lst : 'a list) =
        match lst with
        | head::tail -> if n > 0 then head :: (tail |> take(n-1)) else []
        | _ -> []

    let rec inBatchesOfN n (lst : 'a list) =
        match lst with
        | _::_ -> (lst |> take(n)):: inBatchesOfN n (lst |> skip(n))
        | _ -> []

    [<EntryPoint>]
    let main argv = 
        let on = ref true    
        let data = ref (Array.create 4096 ( new Byte() ))
        let requests = ref []

        let log = LogManager.GetLogger("interceptor")
        let currentBase = System.Configuration.ConfigurationManager.AppSettings.["current"]
        let candidateBase = System.Configuration.ConfigurationManager.AppSettings.["candidate"]
        let relAddresses = List.ofSeq (System.IO.File.ReadLines("testpath.txt"))
        let addressPairs = relAddresses |> List.map (fun adr -> currentBase + adr, candidateBase + adr)
        let addresses = addressPairs |> List.collect (fun (cur, pro) -> [cur; pro])

        printf "To start the test press a key...\r\n"
        Console.ReadKey() |>  ignore

        Omniture.intercept (fun () -> !on) (fun ()-> data) (fun req-> requests := !requests |> List.append [req]; printf "Caught one. Now they are %d.\r\n" (!requests).Length;)
     
        printf "Carrying out test scenario. %d pages will be opened each in 2 environments, requests intercepted.\r\n" (relAddresses.Length)
        addresses
        |> inBatchesOfN 4
        |> List.iter (fun l -> l |> Scenario.run (fun adr -> printf "Starting '%s'\r\n" adr) (fun _ -> printf " -Ready\r\n") (fun () -> printf " -Page load timeout\r\n") (fun () -> printf " -Slideshow click\r\n") (fun t -> printf " -\"%s\" Tab click\r\n" t) (fun () -> printf " -Tab show more click\r\n") (fun () -> printf " -Tab senaste dygnet click\r\n") (fun l -> printf " -\"%s\" Tab link click\r\n" l) (fun () -> printf " Mobile version\r\n")) 

        printf "Test scenario finished. To stop capturing press a key...\r\n"
        Console.ReadKey() |>  ignore

        on := false

        let matches =  Request.matchesForAddressPairs addressPairs !requests
        let numUnmatched = matches.Count(fun (a,_,_,_) -> not a)

        if numUnmatched>0 then 
            printf "Not all requests have been matched. Number of address pairs with unmatched requests was %d.\r\n" numUnmatched
            printf "Failed to intercept expected requests from the following addresses:\r\n"
            List.iter (fun (a,b,c,_) -> if not a then [b; c] |> List.iter writeIfNotEmpty) (matches)
        else 
            printf "All requests matched successfully.\r\n"
        List.iter (fun (a,_,_,b) -> if a then log.Debug b) (matches)
        List.iter (fun req -> log.Debug req) (!requests)
        printf "Matched pairs, requests have been logged.\r\n"
        Console.ReadKey() |>  ignore

        0 //exit code 