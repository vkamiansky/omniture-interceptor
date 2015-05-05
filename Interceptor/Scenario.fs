namespace FirstLine

module Scenario =

    type TestAction =
        | PageOpenBefore of string
        | PageOpenAfter
        | SlideshowNextClickBefore
        | SlideshowNextClickAfter
        | SlideshowPopupClickBefore
        | SlideshowPopupClickAfter
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
        | QuizAnswerClickBefore
        | QuizAnswerClickAfter
        | Error of string
               
    let run doAction urls =
            let go url driver =
                    driver |> Driver.go url
                                (fun adr -> doAction (PageOpenBefore adr))
                                (fun () -> doAction PageOpenAfter)

            Driver.get()
            |> Driver.tryLoop urls 
                    (fun ex -> doAction (Error ex))
                    (fun _  -> ())
                    (fun () -> ())
                    (fun url driver ->
                            driver
                            |> Driver.dropCookies
                            |> go url 
                            |> Driver.findAndDo ".pageGuid[value=\"0bf95c48-b2c4-43d1-8c28-8a02abaf0d38\"]"
                                        (fun _ drv ->
                                                drv
                                                |> Driver.clickAll ".tab-link"
                                                                   (fun tab -> doAction (TabClickBefore tab)) 
                                                                   (fun _ -> doAction TabClickAfter) 
                                                |> Driver.refresh
                                                |> Driver.clickFirst ".b-tabs__tab-content_active .b-link__show-more"
                                                                   (fun _ -> doAction TabShowMoreClickBefore) 
                                                                   (fun _ -> doAction TabShowMoreClickAfter) 
                                                |> Driver.refresh
                                                |> Driver.clickFirst ".b-tabs__tab-content_active .article-link"
                                                                   (fun lnk -> doAction (TabLinkClickBefore lnk)) 
                                                                   (fun _ -> doAction TabLinkClickAfter) 
                                                |> go url
                                                |> Driver.clickFirst ".b-tabs__tab-content_active .b-link__read-more"
                                                                   (fun _ -> doAction TabReadMoreClickBefore) 
                                                                   (fun _ -> doAction TabReadMoreClickAfter) 
                                                |> go url)
                            |> Driver.findVisibleAndDo "iframe[seamless=seamless]" 
                                        (fun frames drv -> 
                                                           drv
                                                           |> Driver.frame (frames |> List.head)
                                                           |> Driver.clickOptionNext 
                                                                        ".list-group-item"
                                                                        ".quiz-button-next,.quiz-button-done"
                                                                        (fun _ -> doAction QuizAnswerClickBefore) 
                                                                        (fun () -> doAction QuizAnswerClickAfter)
                                                           |> Driver.frameUp)
                            |> Driver.clickFirst ".b-slideshow__next" 
                                        (fun _ -> doAction SlideshowNextClickBefore) 
                                        (fun _ -> doAction SlideshowNextClickAfter)
                            |> Driver.findVisibleAndDo ".sb-player" 
                                        (fun _ drv -> doAction VideoDisplay; drv)
                            |> Driver.clickAll ".b-share__facebook,.b-share__twitter,.b-share__mail,.b-share__print" 
                                        (fun btn -> doAction (ShareButtomClickBefore btn)) 
                                        (fun _ -> doAction ShareButtomClickAfter)
                            |> Driver.findAndDo ".b-share__facebook,.b-share__twitter,.b-share__mail,.b-share__print" 
                                        (fun _ drv -> drv |> go url ) 
                            |> Driver.clickFirst ".current[href*=\"'channel', 'Mobile'\"]" 
                                        (fun _ -> doAction SwitchMobileBefore) 
                                        (fun _ -> doAction SwitchMobileAfter)
                            |> Driver.findAndDo ".b-quiz-list"
                                        (fun _ drv ->
                                                drv
                                                |> Driver.clickOptionNext 
                                                                        "input[type=radio]"
                                                                        ".b-quiz__next .b-button"
                                                                        (fun _ -> doAction QuizAnswerClickBefore) 
                                                                        (fun () -> doAction QuizAnswerClickAfter)
                                        )
                            |> Driver.findVisibleAndDo ".sb-player" 
                                        (fun _ drv -> doAction VideoDisplay; drv)
                            |> Driver.clickFirst ".b-page_article .b-slideshow__group-wrap .b-slideshow__open"
                                        (fun _ -> doAction SlideshowPopupClickBefore) 
                                        (fun drv -> drv |> Driver.back |> ignore; doAction SlideshowPopupClickAfter)
                            |> Driver.clickAllFresh ".b-share-btn_fb .b-share-btn__link,.b-share-btn_twttr .b-share-btn__link, .b-share-btn_whatsapp .b-share-btn__link"
                                        (fun btn -> doAction (ShareButtomClickBefore btn)) 
                                        (fun drv -> drv |> Driver.back |> ignore; doAction SlideshowNextClickAfter)
                            |> Driver.clickAll ".b-share-btn_mail .b-share-btn__link" 
                                        (fun btn -> doAction (ShareButtomClickBefore btn)) 
                                        (fun _ ->  doAction ShareButtomClickAfter)
                            |> Driver.findAndDo ".pageGuid[value=\"0bf95c48-b2c4-43d1-8c28-8a02abaf0d38\"]"
                                        (fun _ drv ->
                                                drv
                                                |> Driver.clickAll ".b-tabs-widget__tab-header"
                                                                   (fun tab -> doAction (TabClickBefore tab)) 
                                                                   (fun _ -> doAction TabClickAfter) 
                                                |> Driver.refresh
                                                |> Driver.clickFirst ".b-tabs-widget__tab-content .b-nlist__more"
                                                                   (fun _ -> doAction TabShowMoreClickBefore) 
                                                                   (fun _ -> doAction TabShowMoreClickAfter) 
                                                |> Driver.refresh
                                                |> Driver.clickFirst ".b-tabs-widget__tab-content .b-nlist__link"
                                                                   (fun lnk -> doAction (TabLinkClickBefore lnk)) 
                                                                   (fun _ -> doAction TabLinkClickAfter)
                                         ))
            |> Driver.leave 