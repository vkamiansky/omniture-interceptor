namespace FirstLine
open System
open System.Threading
open OpenQA.Selenium
open OpenQA.Selenium.PhantomJS

module Sut =

    let runScenario doPageOpenStart doPageOpenReady doTimeout doSlideshowClick urls =
        let driverService = PhantomJSDriverService.CreateDefaultService(HideCommandPromptWindow = true)
        let driver = new PhantomJSDriver(driverService)  
        driver.Manage().Timeouts().SetPageLoadTimeout(TimeSpan.FromSeconds(10.)) |>ignore
        let runPageScenario (url:string) =
            try
                doPageOpenStart url
                driver.Navigate().GoToUrl(url)
                doPageOpenReady url
                match driver.FindElementsByClassName("b-slideshow__next") |> List.ofSeq with
                    | slideshowNext :: _ -> doSlideshowClick (); slideshowNext.Click()
                    | [] -> ()
            with
                | :? WebDriverTimeoutException -> doTimeout ()
        urls |> List.iter runPageScenario
        driver.Quit()
        driver.Dispose()