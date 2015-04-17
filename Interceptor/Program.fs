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

        printf "Press Enter to start the test...\r\n"
        Console.ReadKey() |>  ignore

        Omniture.intercept 
                     (fun () -> !on) 
                     (fun ()-> data) 
                     (fun req-> requests := !requests |> List.append [req]; printf "Caught one. Now they are %d.\r\n" (!requests).Length;)
     
        printf "Test scenario started.\r\n"

        let rec repeatRunMatch testRelAddresses =

            testRelAddresses 
            |> List.collect (fun rel -> [currentBase + rel; candidateBase + rel])
            |> List.fragment 4
            |> List.iter 
                (fun l -> l 
                          |> Scenario.run 
                                (fun adr -> printf " Starting '%s'\r\n" adr) 
                                (fun ()  -> printf " -Page load timeout\r\n") 
                                (fun ()  -> printf " -Slideshow click\r\n") 
                                (fun t   -> printf " -\"%s\" Tab click\r\n" t) 
                                (fun ()  -> printf " -Tab show more click\r\n") 
                                (fun ()  -> printf " -Tab senaste dygnet click\r\n") 
                                (fun l   -> printf " -\"%s\" Tab link click\r\n" l) 
                                (fun ()  -> printf " Mobile version\r\n")) 

            let faultyRelAddresses = !requests |> Request.processParamsDiffTexts 
                                                    currentBase 
                                                    candidateBase 
                                                    testRelAddresses
                                                    (fun diff-> log.Debug diff)

            match faultyRelAddresses with
                | faultyLst when faultyLst |> List.length > 0 ->
                       printf "Not all requests have been matched successfully. See the faulty rel addresses below.\r\n"
                       faultyLst |> List.iter (fun adr -> printf "%s\r\n" adr)
                       printf "The test will be run again for the addresses above.\r\n"
                       requests := []
                       faultyLst |> repeatRunMatch 
                | _ -> 
                       on := false
                       printf "All requests matched successfully. See log file for results.\r\n"

        let timeStart = DateTime.Now
        repeatRunMatch relAddresses
        printf "And it all took %f minutes.\r\n" (DateTime.Now.Subtract timeStart).TotalMinutes
        printf "Press Enter to exit...\r\n"
        Console.ReadKey() |>  ignore
        0 //exit code 