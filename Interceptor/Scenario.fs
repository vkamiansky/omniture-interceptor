namespace FirstLine
open System
open System.Threading
open OpenQA.Selenium
open OpenQA.Selenium.Firefox

module Scenario =

    let run doPageOpenStart doPageOpenReady doTimeout doSlideshowClick doTabClick doTabShowMoreClick doTabReadMoreClick doTabLinkClick urls =
        let driver = ref (new FirefoxDriver()) 

        let runPageScenario (url:string) =
            try
                doPageOpenStart url
                (!driver).Navigate().GoToUrl(url)
                Thread.Sleep(2000)
                doPageOpenReady url
                match (!driver).FindElementsByClassName("b-slideshow__next") |> List.ofSeq with
                    | slideshowNext :: _ -> doSlideshowClick (); slideshowNext.Click(); Thread.Sleep(2000) 
                    | [] -> ()
                match (!driver).FindElementsByClassName("pageGuid") |> List.ofSeq with
                    | guid :: _ -> 
                    match guid.GetAttribute("value"), (!driver).FindElementsByClassName("tab-link") |> List.ofSeq with
                        | "0bf95c48-b2c4-43d1-8c28-8a02abaf0d38", tabs when tabs.Length > 0 ->
                              match (!driver).FindElementsByCssSelector(".b-tabs__tab-content_active .b-link__show-more") |> List.ofSeq with
                                  | showMore :: _ -> doTabShowMoreClick(); showMore.Click(); Thread.Sleep(1000)
                                  | [] -> () 
                              tabs |> List.iter (fun t -> doTabClick(t.Text); t.Click(); Thread.Sleep(1000))
                              doPageOpenStart url; (!driver).Navigate().GoToUrl(url); Thread.Sleep(2000); doPageOpenReady url
                              match (!driver).FindElementsByCssSelector(".b-tabs__tab-content_active .article-link") |> List.ofSeq with
                                  | link :: _ -> doTabLinkClick(link.Text); link.Click(); Thread.Sleep(2000)
                                  | [] -> ()
                              doPageOpenStart url; (!driver).Navigate().GoToUrl(url); Thread.Sleep(2000); doPageOpenReady url
                              match (!driver).FindElementsByCssSelector(".b-tabs__tab-content_active .b-link__read-more") |> List.ofSeq with
                                  | readMore :: _ -> doTabReadMoreClick(); readMore.Click(); Thread.Sleep(1000)
                                  | [] -> () 
                        | _ -> ()                    
                    | [] -> ()
                match (!driver).FindElementsByClassName("sb-player") |> List.ofSeq with
                    | video :: _ -> if video.Displayed then Thread.Sleep(90000)  
                    | [] -> ()
            with
                | ex -> (!driver).Quit(); (!driver).Dispose(); driver := new FirefoxDriver(); 
        urls |> List.iter runPageScenario
        (!driver).Quit()
        (!driver).Dispose()