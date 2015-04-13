namespace FirstLine
open System.Diagnostics
open System.Threading

module Sut =

    let browseUrls urls =
        List.iter (fun (adr:string) -> Process.Start(adr)|> ignore; Thread.Sleep(3000)) urls