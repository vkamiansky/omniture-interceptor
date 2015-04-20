namespace FirstLine
open OpenQA.Selenium

module Scenario =

    type TestAction =
        | PageOpenBefore of string
        | PageOpenAfter
        | SlideshowNextClickBefore
        | SlideshowNextClickAfter
        | TabShowMoreClickBefore
        | TabShowMoreClickAfter
        | TabReadMoreClickBefore
        | TabReadMoreClickAfter
        | TabClickBefore of string
        | TabClickAfter
        | TabLinkClickBefore of string
        | TabLinkClickAfter
        | VideoDisplay
        | SwitchMobileBefore
        | SwitchMobileAfter
        | ShareButtomClickBefore of string
        | ShareButtomClickAfter
        | Error of string
   
    let driverGet () =
        new Firefox.FirefoxDriver()

    let driverBrowse (url:string) (driver:Remote.RemoteWebDriver) =
        driver.Navigate().GoToUrl(url)

    let driverRefresh (driver:Remote.RemoteWebDriver) =
        driver.Navigate().Refresh()

    let driverBack (driver:Remote.RemoteWebDriver) =
        driver.Navigate().Back()
    
    let driverFindSelector sel (driver:Remote.RemoteWebDriver) =
        sel |> driver.FindElementsByCssSelector |> List.ofSeq

    let driverCleanup (driver:Remote.RemoteWebDriver) =
        driver.Quit()
        driver.Dispose()

    let switchChannel channel (driver:Remote.RemoteWebDriver) =
        match driver |> driverFindSelector ".current" with
            | switcher when switcher |> List.length > 0 -> 
                        switcher 
                        |> List.tryFind( fun opt -> opt.Text = channel)
                        |> function
                                  | Some opt -> opt.Click();
                                  | None -> ()
            | _ -> ()

    let run doAction urls =
        let driver = ref (driverGet())
        let browse url = doAction (PageOpenBefore url); !driver |> driverBrowse url; doAction PageOpenAfter
        let refresh() = !driver |> driverRefresh
        let back() = !driver |> driverBack
        let findSel sel = !driver |> driverFindSelector sel
        let cleanup() = !driver |> driverCleanup
        let reset() = driver := driverGet()
        let mobile() = doAction SwitchMobileBefore; !driver |> switchChannel "MOBILVERSION"; doAction SwitchMobileAfter

        let rec matchMobileShare i =
            match findSel ".b-share-btn__link" |> List.skip i with
                    | share :: _ -> 
                        doAction (ShareButtomClickBefore share.Text); 
                        if share.Text <> "Mejla" then share.Click(); back() else share.Click(); 
                        doAction ShareButtomClickAfter; matchMobileShare (i+1)
                    | _ -> ()

        let runPageScenario url =
            try
                match findSel ".b-slideshow__next" with
                    | slideshowNext :: _ -> doAction SlideshowNextClickBefore; slideshowNext.Click(); doAction SlideshowNextClickAfter
                    | [] -> ()
                match findSel ".b-share__facebook,.b-share__twitter,.b-share__mail,.b-share__print" with
                    | btns when btns.Length > 0 -> 
                        btns |> List.iter (fun btn -> doAction (ShareButtomClickBefore btn.Text); btn.Click(); doAction ShareButtomClickAfter )
                        browse url
                    | _ -> ()
                matchMobileShare 0
                match findSel ".pageGuid" with
                    | guid :: _ -> 
                        match guid.GetAttribute("value") with
                            | "0bf95c48-b2c4-43d1-8c28-8a02abaf0d38" ->
                                  match findSel ".tab-link" with
                                      | tabs when tabs.Length > 0 ->
                                          match findSel ".b-tabs__tab-content_active .b-link__show-more" with
                                              | showMore :: _ -> doAction TabShowMoreClickBefore; showMore.Click(); doAction TabShowMoreClickAfter;
                                              | [] -> () 
                                          tabs |> List.iter (fun t -> doAction (TabClickBefore t.Text); t.Click(); doAction TabClickAfter)
                                          refresh()  
                                          match findSel ".b-tabs__tab-content_active .article-link" with
                                              | link :: _ -> doAction (TabLinkClickBefore link.Text); link.Click(); doAction TabLinkClickAfter
                                              | [] -> ()
                                          browse url  
                                          match findSel ".b-tabs__tab-content_active .b-link__read-more" with
                                              | readMore :: _ -> doAction TabReadMoreClickBefore; readMore.Click(); doAction TabReadMoreClickAfter;
                                              | [] -> () 
                                          browse url 
                                      | _ -> ()
                                  match findSel ".b-tabs-widget__tab-header" with
                                      | tabs when tabs.Length > 0 -> 
                                          match findSel ".b-tabs-widget__tab-content .b-nlist__more" with
                                              | showMore :: _ -> doAction TabShowMoreClickBefore; showMore.Click(); doAction TabShowMoreClickAfter;
                                              | [] -> () 
                                          tabs |> List.iter (fun t -> doAction (TabClickBefore t.Text); t.Click(); doAction TabClickAfter)
                                          refresh()
                                          match findSel ".b-tabs-widget__tab-content .b-nlist__link" with
                                              | links when links.Length > 0 ->
                                                     links
                                                     |> List.tryFind (fun lnk -> lnk.Displayed)
                                                     |> (function
                                                                | Some l -> doAction (TabLinkClickBefore l.Text); l.Click(); doAction TabLinkClickAfter
                                                                | None -> ())
                                              | _ -> ()
                                      | _ -> ()
                            | _ -> ()                    
                    | [] -> ()
                match findSel ".sb-player" with
                    | video :: _ -> if video.Displayed then doAction VideoDisplay
                    | [] -> ()
            with
                | ex -> doAction (Error ex.Message); (cleanup >> reset)()

        let runScenarioBothPlatforms url =
            (!driver).Manage().Cookies.DeleteAllCookies()
            browse url 
            runPageScenario url
            mobile()
            runPageScenario url

        urls |> List.iter runScenarioBothPlatforms
        cleanup()