namespace FirstLine
open System
open System.Collections.Generic
open System.Collections.Specialized
open System.Diagnostics
open System.Linq
open System.Net
open System.Net.Sockets
open System.IO
open System.Text
open System.Web
open log4net
open log4net.Config
open Net.Data 

module interceptor =

    let log = LogManager.GetLogger("interceptor")
    
    ///<summary>
    /// Finds requests that contains the given link and orders them by length
    ///</summary>
    ///<param name="stream">Requests</param>
    ///<param name="adr">Link</param>
    let findRequestWithAddress (stream:string list) (adr:string) =
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
    ///<param name="str">String request</param>
    let getRequestToUrlParams (str:string) =
        match str with
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
    ///<param name="currentAddress">link to current environment</param>
    ///<param name="candidateAddress">link to candidate environment</param>
    let rec addressPairs relAddresses currentAddress candidateAddress =
        match relAddresses with
        | head :: tail -> (currentAddress + head, candidateAddress + head) :: addressPairs tail currentAddress candidateAddress
        | [] -> []

    ///<summary>
    /// Recieves pairs of encoded links and prints out juxtaposed requests containing those links
    ///</summary>
    ///<param name="pairs">List of coupled web addresses to seek in requests</param>
    ///<param name="stream">List of requests</param>
    let rec matchResults pairs (stream:string list) =
        match pairs with
        | (a, b) :: tail -> 
            let firstRequest = findRequestWithAddress stream a
            let secondRequest = findRequestWithAddress stream b
            match firstRequest, secondRequest with
            | [], [] -> (true, a, b, "") :: matchResults tail stream
            | [], _ -> (false, a, "", "") :: matchResults tail stream
            | _, [] -> (false, "", b, "") :: matchResults tail stream
            | first, second when first.Length = second.Length ->   
                    let pairsFromOneAddress = List.zip first second

                    let toParamRecord (param, cur , pro) =
                         if cur<>pro then String.Format("param: {0}\r\ncurrent:  {1}\r\nproposed: {2}\r\n", param, cur, pro) else ""
                    
                    let paramRecordsToText lst =
                         List.fold (fun acc dat -> acc + (toParamRecord dat)) "\r\n=====\r\n" lst

                    let toParamsText left right = 
                         (getRequestToUrlParams left, getRequestToUrlParams right) ||> juxtapose |> paramRecordsToText 
                                        
                    let aggregatedText = List.fold (fun acc (left, right) -> acc + (toParamsText left right) ) "" pairsFromOneAddress

                    (true, "", "", aggregatedText) :: matchResults tail stream
            | first, second when first.Length > second.Length -> (false, "", b, "") :: matchResults tail stream
            | first, second when first.Length < second.Length -> (false, a, "", "") :: matchResults tail stream
            | _, _ -> (true, a, b, "") :: matchResults tail stream
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
        
        let rec reacieved (ar: IAsyncResult) =

            match new IPHeader(data.Value, data.Value.Length) with
            | ipHeader when ipHeader.ProtocolType = Protocol.TCP -> 
                match new TCPHeader(ipHeader.Data, (int)ipHeader.MessageLength) with
                | header when header.DestinationPort = "80" -> 
                     match Encoding.ASCII.GetString(header.Data).Replace(char(0).ToString(),"") with
                     | req when req.Contains("GET /b/ss/exabdev") -> requestAccum := req
                     | req when (!requestAccum |> String.IsNullOrWhiteSpace |> not) ->
                           if header.MessageLength >= 1460us then
                                requestAccum := !requestAccum + req
                           else
                                requests := List.append !requests [!requestAccum + req]; requestAccum := ""
                                Console.WriteLine("caught one. now they're {0}", (!requests).Length);
                     | _ -> ()
                | _ -> ()
            | _ -> ()

            data := Array.create 4096 ( new Byte() ) 
            if !on then mainSocket.BeginReceive(data.Value, 0, 4096, SocketFlags.None, new AsyncCallback(reacieved), null)  |> ignore        
        
        Console.WriteLine("To start capturing press a key...")
        Console.ReadKey() |>  ignore
        Console.WriteLine("Starting pages automatically. Expected to intercept at least {0} requests.", addresses.Length*2)

        mainSocket.BeginReceive(data.Value, 0, 4096, SocketFlags.None, new AsyncCallback(reacieved), null) |> ignore
            
        let browseUrls (urls: string list) =
           List.iter (fun (adr:string) -> Process.Start(adr)|> ignore; Threading.Thread.Sleep(3000)) urls

        ((List.map (fun adr -> (currentAddress + adr)) addresses),(List.map (fun adr -> (candidateAddress + adr)) addresses)) 
        ||> List.append
        |> browseUrls         

        Console.WriteLine("All pages started. To stop capturing press a key...")
        Console.ReadKey() |>  ignore

        on := false

        let matches = matchResults (addressPairs addresses currentAddress candidateAddress) (!requests)

        Console.WriteLine("+++++")
        Console.WriteLine("Total omniture requests intercepted {0}.", (!requests).Length)
        Console.WriteLine("Number of unmatched request pairs {0}. We expected to intercept & match {1} pairs.", matches.Count(fun (a,_,_,_) -> not a), matches.Count())
        Console.WriteLine("Failed to intercept requests from addresses:")
        List.iter (fun (a,b,c,_) -> if not a then Console.WriteLine("{0}\r\n{1}", b, c)) (matches)
        Console.WriteLine("Matched pairs and requests logged.")
        List.iter (fun (a,_,_,b) -> if a then log.Debug b) (matches)
        List.iter (fun req -> log.Debug req) (!requests)
        Console.ReadKey() |>  ignore

        0 //exit code 