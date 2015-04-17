namespace FirstLine
open System
open System.Collections.Specialized
open System.Linq
open System.Web

module Request =

    let findWithReferer (adr:string) (requests:string list) =
        let adrLower = adr.ToLowerInvariant()
        requests
        |> List.choose (function
                               | request when request.Contains("Referer: " + adrLower + "\r\n") -> Some request
                               | _ -> None)

    let findWithRefererMulEndings adr endings requests =
        endings 
        |> List.collect (fun ending -> requests |> findWithReferer (adr + ending))

    let toUrlParams (request:string) =
        match request with
            | null -> "" 
            | req when req.Contains("GET") && req.Contains("HTTP") ->
                 let adr = req.Split([|"GET "; "HTTP"|],StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
                 match adr with
                     | null -> ""
                     | adr when adr.Length > 0 -> adr.SkipWhile(fun chr -> chr<>'?').Skip(1) |> String.Concat
                     | adr -> "" 
            | req -> ""
        |> HttpUtility.ParseQueryString

    let toJuxtaposedParams (params1:NameValueCollection) (params2:NameValueCollection) =
        (params1.AllKeys, params2.AllKeys) 
        |> Enumerable.Union |> List.ofSeq
        |> List.map (fun key -> key, params1.[key], params2.[key])

    let appendParamDiffText acc (param, curValue , proValue) =
        if curValue<>proValue then acc + sprintf "param: %s\r\ncurrent:  %s\r\nproposed: %s\r\n" param curValue proValue else acc
       
    let appendParamsDiffText acc (curParams, proParams) = 
        (curParams, proParams)
        ||> toJuxtaposedParams
        |> List.fold appendParamDiffText "\r\n=====\r\n"
        |> (+) acc

    let calcParamsDiffText requests (curAddress, proAddress) =
        let platformEndings = [""; "?site=mobile"; "&site=mobile"]
        match (curAddress, proAddress) with
            | (a, b) -> 
                let requestsAddressA = requests |> findWithRefererMulEndings  a  platformEndings
                let requestsAddressB = requests |> findWithRefererMulEndings  b  platformEndings
                match requestsAddressA, requestsAddressB with
                    | [], [] -> None 
                    | [], _  -> None
                    | _, []  -> None 
                    | first, second when first.Length = second.Length ->  
                                                    
                            let aggregatedText = 
                                (first, second)
                                ||> List.map2 (fun f s -> f |> toUrlParams, s |> toUrlParams)
                                |> List.fold appendParamsDiffText "" 

                            Some aggregatedText
                    | _, _ -> None

    let processParamsDiffTexts currentBase candidateBase relAddresses doProcessText requests =
        relAddresses 
        |> List.map (fun adr -> (currentBase + adr, candidateBase + adr) 
                                |> calcParamsDiffText requests
                                |> function
                                          | Some diff -> doProcessText diff; None
                                          | None -> Some adr)
        |> List.choose (fun unmatched -> unmatched)
        