module AndroidExtensions
open System
open System.Collections.Generic
open System.Linq
open System.Text
open System.Threading
open Extensions

open Android.App
open Android.Content
open Android.OS
open Android.Runtime
open Android.Util
open Android.Views
open Android.Widget
open Android.Support.Wearable.Views
open Android.Gms.Common.Apis
open Android.Gms.Common.Data
open Android.Gms.Wearable
open Android.Hardware
open Android.Gms.Common.Apis


let mapRunning serviceInfos =
    let ctx = Android.App.Application.Context
    let amgr = ctx.GetSystemService(Context.ActivityService) :?> ActivityManager
    let running = 
        amgr.GetRunningServices(1000)
        |> Seq.filter (fun ser -> ser.Started)
        |> Seq.map (fun ser -> ser.Service.ClassName)
        |> Seq.map lowercase
        |> Seq.toArray
    let runningSet = running |> Set.ofArray
    let areRunning = serviceInfos |> List.map(fun (name,_) -> name, runningSet |> Set.contains name)
    areRunning

let isServiceRunning serviceName = 
    mapRunning [lowercase serviceName, true]
    |> List.map snd
    |> Seq.nth 0

let notifyAsync 
    (context:Context) 
    (uiCtx:SynchronizationContext)
    (title:string)
    (msg:string)
    fOk =
    async {
        try
            let okHndlr = EventHandler<DialogClickEventArgs>(fOk)
            do! Async.SwitchToContext uiCtx
            use builder = new AlertDialog.Builder(context)
            builder.SetTitle(title)                      |> ignore
            builder.SetMessage(msg)                      |> ignore
            builder.SetCancelable(true)                  |> ignore
            builder.SetPositiveButton("OK", okHndlr)     |> ignore
            builder.Show() |> ignore
        with ex ->  logE ex.Message
    }
    |> Async.Start

let sendWearMessage (x:Context) path data =
    async {
        try
        let bldr = new GoogleApiClientBuilder(x)
        let gapi = bldr.AddApi(WearableClass.Api).Build()
        let r = gapi.BlockingConnect(30L,Java.Util.Concurrent.TimeUnit.Seconds)
        if r.IsSuccess then
            let pr = WearableClass.NodeApi.GetConnectedNodes(gapi)
            let nr =  pr.Await().JavaCast<INodeApiGetConnectedNodesResult>()
            for n in nr.Nodes do
                let pr2 = WearableClass.MessageApi.SendMessage(gapi,n.Id,path,data)
                let s = pr2.Await(1L,Java.Util.Concurrent.TimeUnit.Seconds)
                let r = s.JavaCast<Android.Gms.Wearable.IMessageApiSendMessageResult>()
                if not r.Status.IsSuccess then
                    logI (sprintf "msg send error %d" r.Status.StatusCode)
                    logI r.Status.StatusMessage
        with ex -> 
                logE ex.Message
    }
