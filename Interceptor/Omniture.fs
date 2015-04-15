namespace FirstLine
open System
open System.Net
open System.Net.Sockets
open System.Text
open Net.Data 

module Omniture =
    
    let intercept calcGoOn calcRefBytes doAppendRequest =

        let refBytes = calcRefBytes()
        let refBytesValue = !calcRefBytes()

        let requestAccum = ref ""

        let mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP)
        let ip = Dns.GetHostEntry((Dns.GetHostName())).AddressList
                 |> List.ofSeq
                 |> List.tryFind (fun adr -> adr.AddressFamily = AddressFamily.InterNetwork) 
                 |> function
                    | Some adr -> adr
                    | None -> null

        mainSocket.Bind(new IPEndPoint(ip, 0));
        mainSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true)
        let control = mainSocket.IOControl(IOControlCode.ReceiveAll, [|byte(1);byte(0);byte(0);byte(0)|], [|byte(1);byte(0);byte(0);byte(0)|]) 
        
        let rec received (ar: IAsyncResult) =
            match new IPHeader(refBytesValue, refBytesValue.Length) with
            | ipHeader when ipHeader.ProtocolType = Protocol.TCP -> 
                match new TCPHeader(ipHeader.Data, (int)ipHeader.MessageLength) with
                | header when header.DestinationPort = "80" -> 
                        match Encoding.ASCII.GetString(header.Data).Replace(char(0).ToString(),"") with
                        | req when req.Contains("GET /b/ss/exabdev") && (req.Contains("pccr")|>not) && (req.Contains("lnk_e")|>not) -> requestAccum := req
                        | req when (!requestAccum |> (String.IsNullOrWhiteSpace >> not)) ->
                            if header.MessageLength >= 1460us then
                                requestAccum := !requestAccum + req
                            else
                                doAppendRequest (!requestAccum + req); requestAccum := ""
                        | _ -> ()
                | _ -> ()
            | _ -> ()
            refBytes := Array.create 4096 ( new Byte() ) 
            if calcGoOn() then mainSocket.BeginReceive(refBytesValue, 0, 4096, SocketFlags.None, new AsyncCallback(received), null)  |> ignore        

        mainSocket.BeginReceive(refBytesValue, 0, 4096, SocketFlags.None, new AsyncCallback(received), null)  |> ignore                       