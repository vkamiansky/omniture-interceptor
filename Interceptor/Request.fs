namespace FirstLine
open System
open System.Collections.Specialized
open System.Linq
open System.Web

module Request =

    let findWithReferer (adr:string) (requests:string list) =
        let adrLower = adr.ToLowerInvariant()
        List.choose (fun (str:string) -> if str.Contains("Referer: " + adrLower + "\r\n") then Some str else None ) (requests)

    let findWithRefererMulEndings (adr:string) endings (requests:string list) =
        endings |> List.collect (fun e -> requests |> findWithReferer (adr + e))

    let juxtapose (params1:NameValueCollection) (params2:NameValueCollection) =
        let unionKeys = (params1.AllKeys, params2.AllKeys) |> Enumerable.Union
        List.map (fun (str:string) -> (str, params1.Item(str), params2.Item(str))) (unionKeys |> List.ofSeq)

    let toUrlParams (request:string) =
        match request with
        | null -> "" 
        | str when str.Contains("GET") && str.Contains("HTTP") ->
             let adr = str.Split([|"GET "; "HTTP"|],StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
             match adr with
             | null -> ""
             | adr when adr.Length > 0 -> adr.SkipWhile(fun chr -> chr<>'?').Skip(1) |> String.Concat
             | adr -> "" 
        | str -> ""
        |> HttpUtility.ParseQueryString

    let appendParamDiffText acc (param, curValue , proValue) =
        if curValue<>proValue then acc + sprintf "param: %s\r\ncurrent:  %s\r\nproposed: %s\r\n" param curValue proValue else acc
       
    let appendParamsDiffText acc (curParams, proParams) = 
        (juxtapose curParams proParams)
        |> List.fold appendParamDiffText "\r\n=====\r\n"
        |> (+) acc

    let rec matchesForAddressPairs addressPairs requests =
        let platformEndings = [""; "?site=mobile"; "&site=mobile"]
        match addressPairs with
        | (a, b) :: tail -> 
            let requestsAddressA = requests |> findWithRefererMulEndings  a  platformEndings
            let requestsAddressB = requests |> findWithRefererMulEndings  b  platformEndings
            match requestsAddressA, requestsAddressB with
            | [], [] -> (false, a, b, "") :: matchesForAddressPairs tail requests
            | [], _ -> (false, a, "", "") :: matchesForAddressPairs tail requests
            | _, [] -> (false, "", b, "") :: matchesForAddressPairs tail requests
            | first, second when first.Length = second.Length ->  
                                                    
                    let aggregatedText = 
                        (first, second)
                        ||> List.map2 (fun f s -> f |> toUrlParams, s |> toUrlParams)
                        |> List.fold appendParamsDiffText "" 

                    (true, "", "", aggregatedText) :: matchesForAddressPairs tail requests
            | first, second when first.Length > second.Length -> (false, "", b, "") :: matchesForAddressPairs tail requests
            | first, second when first.Length < second.Length -> (false, a, "", "") :: matchesForAddressPairs tail requests
            | _, _ -> (false, a, b, "") :: matchesForAddressPairs tail requests
        | [] -> []