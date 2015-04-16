namespace FirstLine
open System
open System.Diagnostics
open System.Threading
open OpenQA.Selenium.Remote
open OpenQA.Selenium.Firefox

module Scenario =
   
    let driverGet () =
        new FirefoxDriver()

    let driverBrowse (url:string) doPageOpenStart doPageOpenReady (driver:RemoteWebDriver) =
        doPageOpenStart url
        driver.Navigate().GoToUrl(url)
        Thread.Sleep(1000)
        doPageOpenReady url
    
    let driverFindSelector sel (driver:RemoteWebDriver) =
        sel |> driver.FindElementsByCssSelector |> List.ofSeq

    let driverCleanup (driver:RemoteWebDriver)  =
        driver.Quit()
        driver.Dispose()

    let run doPageOpenStart doPageOpenReady doTimeout doSlideshowClick doTabClick doTabShowMoreClick doTabReadMoreClick doTabLinkClick urls =
        let driver = ref (driverGet())
        let browse url = !driver |> driverBrowse url doPageOpenStart doPageOpenReady
        let findSel sel = !driver |> driverFindSelector sel
        let cleanup() = !driver |> driverCleanup
        let reset() = driver := driverGet()

        let runPageScenario (url:string) =
            try
                browse url 
                match findSel ".b-slideshow__next" with
                    | slideshowNext :: _ -> doSlideshowClick (); slideshowNext.Click(); Thread.Sleep(1000) 
                    | [] -> ()
                match findSel ".pageGuid" with
                    | guid :: _ -> 
                    match guid.GetAttribute("value"), findSel ".tab-link" with
                        | "0bf95c48-b2c4-43d1-8c28-8a02abaf0d38", tabs when tabs.Length > 0 ->
                              match findSel ".b-tabs__tab-content_active .b-link__show-more" with
                                  | showMore :: _ -> doTabShowMoreClick(); showMore.Click(); Thread.Sleep(1000)
                                  | [] -> () 
                              tabs |> List.iter (fun t -> doTabClick(t.Text); t.Click(); Thread.Sleep(1000))
                              browse url  
                              match findSel ".b-tabs__tab-content_active .article-link" with
                                  | link :: _ -> doTabLinkClick(link.Text); link.Click(); Thread.Sleep(1000)
                                  | [] -> ()
                              browse url  
                              match findSel ".b-tabs__tab-content_active .b-link__read-more" with
                                  | readMore :: _ -> doTabReadMoreClick(); readMore.Click(); Thread.Sleep(1000)
                                  | [] -> () 
                        | _ -> ()                    
                    | [] -> ()
                match findSel ".sb-player" with
                    | video :: _ -> if video.Displayed then Thread.Sleep(90000)  
                    | [] -> ()
            with
                | ex -> (cleanup >> reset)()

        urls |> List.iter runPageScenario
        cleanup()