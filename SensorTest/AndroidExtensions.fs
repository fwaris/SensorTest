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
        with ex -> logE (sprintf  "notifyAsync %s" ex.Message)
    }
    |> Async.Start

let promptAsync 
    (context:Context) 
    (uiCtx:SynchronizationContext)
    (title:string)
    (msg:string)
    fOk
    fCancel =
    async {
        try
            let okHndlr = EventHandler<DialogClickEventArgs>(fOk)
            let cnHndlr = EventHandler<DialogClickEventArgs>(fCancel)
            do! Async.SwitchToContext uiCtx
            use builder = new AlertDialog.Builder(context)
            builder.SetTitle(title)                      |> ignore
            builder.SetMessage(msg)                      |> ignore
            builder.SetCancelable(true)                  |> ignore
            builder.SetPositiveButton("OK", okHndlr)     |> ignore
            builder.SetNegativeButton("Cancel", cnHndlr) |> ignore
            builder.Show() |> ignore 
        with ex -> logE (sprintf "promptAsync %s" ex.Message)
    }
    |> Async.Start

let toastShort (msg:string) =
    if Android.OS.Looper.MyLooper() = null then
        Android.OS.Looper.Prepare()
    try
        Toast.MakeText(Android.App.Application.Context,msg, ToastLength.Short).Show() |> ignore
    with ex ->
        let ex2 = ex.InnerException
        logE ex.Message
