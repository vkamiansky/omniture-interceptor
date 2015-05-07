namespace FirstLine
open System
open System.Diagnostics
open System.Threading
open log4net

module interceptor =

    let minutesSince startTime =
        startTime |> function
            | timePoint when DateTime.Now > timePoint -> (DateTime.Now.Subtract timePoint).TotalMinutes
            | _ -> -1.

    [<EntryPoint>]
    let main argv = 
        let on = ref true    
        let data = ref (Array.create 4096 ( new Byte() ))
        let requests = ref []

        let log = LogManager.GetLogger("interceptor")
        let currentBase = System.Configuration.ConfigurationManager.AppSettings.["current"]
        let candidateBase = System.Configuration.ConfigurationManager.AppSettings.["candidate"]
        let relAddresses = System.IO.File.ReadLines("testpath.txt") |> List.ofSeq

        let rec repeatRunMatch startTime testRelAddresses =

            testRelAddresses 
            |> List.collect (fun rel -> [currentBase + rel; candidateBase + rel])
            |> List.fragment 4
            |> List.iter 
                (fun l -> l 
                          |> Scenario.run 
                                  (function
                                    | Scenario.PageOpenBefore adr -> printf " Starting '%s'\r\n" adr
                                    | Scenario.SlideshowNextClickBefore -> printf " -Slideshow Next click\r\n"
                                    | Scenario.SlideshowPopupClickBefore -> printf " -Slideshow popup click\r\n"
                                    | Scenario.TabShowMoreClickBefore -> printf " -Tab show more click\r\n"
                                    | Scenario.TabReadMoreClickBefore -> printf " -Tab senaste dygnet click\r\n"
                                    | Scenario.TabClickBefore tab -> printf " -\"%s\" Tab click\r\n" tab
                                    | Scenario.TabLinkClickBefore lnk -> printf " -\"%s\" Tab link click\r\n" lnk
                                    | Scenario.SwitchMobileBefore -> printf " Mobile version\r\n"
                                    | Scenario.ShareButtomClickBefore btn -> printf " -\"%s\" Share button click\r\n" btn
                                    | Scenario.QuizAnswerClickBefore -> printf " Quiz answer\r\n"
                                    | Scenario.Error msg -> printf " -Error: %s\r\n" msg; log.Debug msg
                                    | Scenario.VideoDisplay -> Thread.Sleep 90000
                                    | _ -> Thread.Sleep 1000
                                    ))
                                   
            !requests 
            |> Request.processParamsDiffTexts currentBase candidateBase testRelAddresses (fun diff -> log.Debug diff)
            |> function
                | faultyAddrLst when faultyAddrLst |> List.length > 0 ->
                       printf "Not all requests have been matched successfully. See the faulty rel addresses below.\r\n"
                       faultyAddrLst |> List.iter (fun adr -> printf "/%s\r\n" adr)
                       printf "The test will be rerun for the addresses above.\r\n"
                       requests := []
                       faultyAddrLst |> repeatRunMatch startTime
                | _ -> 
                       on := false
                       printf "All requests matched successfully in %f minutes.\r\n" (minutesSince startTime)

        printf "Press any key to start the test...\r\n"
        Console.ReadKey() |>  ignore

        Omniture.intercept 
                     (fun () -> !on) 
                     (fun ()-> data) 
                     (fun req-> requests := (!requests, [req]) ||> List.append; printf "Caught one. Now they are %d.\r\n" (!requests).Length;)

        printf "Starting test scenario...\r\n"
        relAddresses |> repeatRunMatch DateTime.Now
                
        printf "Press any key to open the resulting log file...\r\n"
        Console.ReadKey() |>  ignore
        log.Logger.Repository.GetAppenders() |> function
            | app when app.Length > 0 -> app.[0] |> function
                | :? Appender.FileAppender as fap -> Process.Start fap.File |> ignore
                | _ -> ()
            | _ -> ()
        0 //exit code 