namespace FirstLine
open System
open System.Collections.Specialized
open System.Diagnostics
open System.Linq
open System.Net
open System.Net.Sockets
open System.IO
open System.Text
open System.Web
open log4net
open Net.Data 

module interceptor =

    let log = LogManager.GetLogger("interceptor")
    
    ///<summary>
    /// Finds requests sent from the following address and returns them in their original order
    ///</summary>
    ///<param name="stream">All requests</param>
    ///<param name="adr">Address</param>
    let findRequestsWithAddress (stream:string list) (adr:string) =
        let adrLower = adr.ToLowerInvariant()
        List.choose (fun (str:string) -> if str.Contains("Referer: " + adrLower + "\r\n") then Some str else None ) (stream)

    ///<summary>
    /// Merges keys from 2 collections and for each one returns a triade
    /// made up of the key itself and its representations in the 2 collections
    ///</summary>
    ///<param name="params1">Collection 1</param>
    ///<param name="params2">Collection 2</param>
    let juxtapose (params1:NameValueCollection) (params2:NameValueCollection) =
        let unionKeys = (params1.AllKeys, params1.AllKeys) |> Enumerable.Union
        List.map (fun (str:string) -> (str, params1.Item(str), params2.Item(str))) (unionKeys |> List.ofSeq)

    ///<summary>
    /// Retrieves params from request
    ///</summary>
    ///<param name="req">String request</param>
    let getRequestToUrlParams (req:string) =
        match req with
        | null -> "" 
        | str when str.Contains("GET") && str.Contains("HTTP") ->
             let adr = str.Split([|"GET "; "HTTP"|],StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
             match adr with
             | null -> ""
             | adr when adr.Length > 0 -> adr.SkipWhile(fun chr -> chr<>'?').Skip(1) |> String.Concat
             | adr -> "" 
        | str -> ""
        |> HttpUtility.ParseQueryString

    ///<summary>
    /// Returns pairs of links for representing given rel paths in 2 environments
    ///</summary>
    ///<param name="relAddresses">List of relative page paths</param>
    ///<param name="currentAddress">base url of the current environment</param>
    ///<param name="candidateAddress">base url of the candidate environment</param>
    let rec addressPairs relAddresses currentAddress candidateAddress =
        match relAddresses with
        | head :: tail -> (currentAddress + head, candidateAddress + head) :: addressPairs tail currentAddress candidateAddress
        | [] -> []

    ///<summary>
    /// Appends accumulated text with info about differing param values.
    /// If values equal appends nothing
    ///</summary>
    ///<param name="acc">String accumulator</param>
    ///<param name="param">param name</param>
    ///<param name="cur">value in current env</param>
    ///<param name="pro">value in proposed env</param>
    let appendParamDiffText acc (param, cur , pro) =
        if cur<>pro then acc + sprintf "param: %s\r\ncurrent:  %s\r\nproposed: %s\r\n" param cur pro else acc
       
    ///<summary>
    /// Appends accumulated text with juxtaposed params for a pair of requests
    /// Only adds params values of which differ in the two requests
    ///</summary>
    ///<param name="acc">String accumulator</param>
    ///<param name="cur">request from current env</param>
    ///<param name="cur">request from proposed env</param>
    let appendRequestPairParamsText acc (cur, pro) = 
        (getRequestToUrlParams cur) 
        |> juxtapose (getRequestToUrlParams pro)
        |> List.fold appendParamDiffText "\r\n=====\r\n"
        |> (+) acc

    ///<summary>
    /// Recieves pairs links and returns a triade comprising
    /// a bool value showing if the pair has matching sets of requests
    /// then if unmatched the two faulty addresses with empty strings in place of non-faulty ones and a string of 
    /// juxtaposed differing params of requests sent from those addresses in a readable form
    ///</summary>
    ///<param name="addressPairs">List of coupled web addresses the requests were supposed to be caught from</param>
    ///<param name="requests">List of all caught requests</param>
    let rec matchResults addressPairs requests =
        match addressPairs with
        | (a, b) :: tail -> 
            let firstRequest = findRequestsWithAddress requests a
            let secondRequest = findRequestsWithAddress requests b
            match firstRequest, secondRequest with
            | [], [] -> (false, a, b, "") :: matchResults tail requests
            | [], _ -> (false, a, "", "") :: matchResults tail requests
            | _, [] -> (false, "", b, "") :: matchResults tail requests
            | first, second when first.Length = second.Length ->  
                                                    
                    let aggregatedText = 
                        first
                        |> List.zip second
                        |> List.fold appendRequestPairParamsText "" 

                    (true, "", "", aggregatedText) :: matchResults tail requests
            | first, second when first.Length > second.Length -> (false, "", b, "") :: matchResults tail requests
            | first, second when first.Length < second.Length -> (false, a, "", "") :: matchResults tail requests
            | _, _ -> (false, a, b, "") :: matchResults tail requests
        | [] -> []

    [<EntryPoint>]
    let main argv = 

        let on = ref true    
        let data = ref (Array.create 4096 ( new Byte() ))
        let requestAccum = ref ""
        let requests = ref []

        let currentAddress = System.Configuration.ConfigurationManager.AppSettings.["current"]
        let candidateAddress = System.Configuration.ConfigurationManager.AppSettings.["candidate"]
        let addresses = List.ofSeq (System.IO.File.ReadLines("testpath.txt"))

        let mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP)
        let ip = Dns.GetHostEntry((Dns.GetHostName())).AddressList.First(fun adr -> adr.AddressFamily = AddressFamily.InterNetwork)
        
        mainSocket.Bind(new IPEndPoint(ip, 0));
        mainSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true)
        let control = mainSocket.IOControl(IOControlCode.ReceiveAll, [|byte(1);byte(0);byte(0);byte(0)|], [|byte(1);byte(0);byte(0);byte(0)|])
        
        let rec received (ar: IAsyncResult) =
            match new IPHeader(data.Value, data.Value.Length) with
            | ipHeader when ipHeader.ProtocolType = Protocol.TCP -> 
                match new TCPHeader(ipHeader.Data, (int)ipHeader.MessageLength) with
                | header when header.DestinationPort = "80" -> 
                     match Encoding.ASCII.GetString(header.Data).Replace(char(0).ToString(),"") with
                     | req when req.Contains("GET /b/ss/exabdev") -> requestAccum := req
                     | req when (!requestAccum |> (String.IsNullOrWhiteSpace >> not)) ->
                           if header.MessageLength >= 1460us then
                                requestAccum := !requestAccum + req
                           else
                                requests := List.append !requests [!requestAccum + req]; requestAccum := ""
                                printf "caught one. now they're %d\r\n" (!requests).Length;
                     | _ -> ()
                | _ -> ()
            | _ -> ()
            data := Array.create 4096 ( new Byte() ) 
            if !on then mainSocket.BeginReceive(data.Value, 0, 4096, SocketFlags.None, new AsyncCallback(received), null)  |> ignore        

        let browseUrls urls =
           List.iter (fun (adr:string) -> Process.Start(adr)|> ignore; Threading.Thread.Sleep(3000)) urls

        let appendAbsoluteAddresses baseAdr relAddresses acc =
           relAddresses
           |> List.map (fun rel -> (baseAdr + rel))
           |> List.append acc

        let writeIfNotEmpty str = 
           if str |> (String.IsNullOrEmpty >> not) then printf "%s\r\n" str

        printf "To start capturing press a key...\r\n"
        Console.ReadKey() |>  ignore
        printf "Starting pages automatically. Expected to intercept at least %d requests.\r\n" (addresses.Length * 2)

        mainSocket.BeginReceive(data.Value, 0, 4096, SocketFlags.None, new AsyncCallback(received), null) |> ignore

        []
        |> appendAbsoluteAddresses currentAddress addresses
        |> appendAbsoluteAddresses candidateAddress addresses
        |> browseUrls        

        printf "All pages started. To stop capturing press a key...\r\n"
        Console.ReadKey() |>  ignore

        on := false

        let matches = matchResults (addressPairs addresses currentAddress candidateAddress) (!requests)
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