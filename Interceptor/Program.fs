namespace FirstLine
open System
open System.Linq
open log4net

module interceptor =

    let writeIfNotEmpty str = 
        if str |> (String.IsNullOrEmpty >> not) then printf "%s\r\n" str

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

        printf "To start capturing press a key...\r\n"
        Console.ReadKey() |>  ignore
        Omniture.intercept (fun () -> !on) (fun ()-> data) (fun req-> requests := !requests |> List.append [req]; printf "Caught one. Now they are %d.\r\n" (!requests).Length;)
     
        printf "Starting pages automatically. %d pages will be opened each in 2 environments.\r\n" (relAddresses.Length)
        addressPairs |> List.iter (fun (cur, pro)-> [cur; pro] |> Sut.browseUrls) 
        printf "All pages started. To stop capturing press a key...\r\n"
        Console.ReadKey() |>  ignore

        on := false

        let matches =  Request.matchesForAddressPairs addressPairs !requests
        let numUnmatched = matches.Count(fun (a,_,_,_) -> not a)

        if numUnmatched>0 then 
            printf "Not all requests have been matched. Number of address pairs with unmatched requests was %d.\r\n" numUnmatched
            printf "Failed to intercept expected requests from the following addresses:\r\n"
            List.iter (fun (a,b,c,_) -> if not a then (writeIfNotEmpty b; writeIfNotEmpty c;)) (matches)
        else 
            printf "All requests matched successfully.\r\n"
        List.iter (fun (a,_,_,b) -> if a then log.Debug b) (matches)
        List.iter (fun req -> log.Debug req) (!requests)
        printf "Matched pairs, requests have been logged.\r\n"
        Console.ReadKey() |>  ignore

        0 //exit code 