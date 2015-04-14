namespace FirstLine
open System
open System.Threading
open OpenQA.Selenium.Firefox

module Sut =

    let runScenario doPageOpenStart doPageOpenReady doTimeout doSlideshowClick urls =
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
                match (!driver).FindElementsByClassName("sb-player") |> List.ofSeq with
                    | video :: _ -> if video.Displayed then Thread.Sleep(90000)  
                    | [] -> ()
            with
                | _-> (!driver).Quit(); (!driver).Dispose(); driver := new FirefoxDriver()
        urls |> List.iter runPageScenario
        (!driver).Quit()
        (!driver).Dispose()