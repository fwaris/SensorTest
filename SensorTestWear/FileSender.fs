module FileSender

open Extensions
open System
open System.Collections.Generic
open System.Linq
open System.Text
open System.IO
open System.Threading

open Android.App
open Android.Content
open Android.OS
open Android.Runtime
open Android.Views
open Android.Widget
open Android.Gms.Wearable
open Android.Hardware
open Android.Gms.Common.Apis
open Android.Gms.Common

type ResultCB(fDone) =
    inherit Java.Lang.Object() 
    interface IResultCallback with
        member x.OnResult(result) =
            let r = result.JavaCast<IDataApiDataItemResult>();
            fDone r.Status

let sendFile (ctx:Context) apiClient filePath =
    logI ("sending file " + filePath)
    async {
        try
            let dataRequest = PutDataRequest.Create(Constants.p_data_file)
            let fileName = Path.GetFileName(filePath)
            let contentUri = Android.Net.Uri.Parse(Storage.contentUri fileName)
            let permissionFlags = ActivityFlags.GrantReadUriPermission ||| ActivityFlags.GrantPersistableUriPermission
            let plyServPkg = GooglePlayServicesUtil.GooglePlayServicesPackage
            ctx.GrantUriPermission(plyServPkg,contentUri,permissionFlags)
            let asset = Asset.CreateFromUri(contentUri)
            dataRequest.PutAsset(fileName,asset) |> ignore
            let ev = new ManualResetEvent(false)
            let result = ref (Some "waiting")
            let pendingRequest = WearableClass.DataApi.PutDataItem(apiClient,dataRequest)
            pendingRequest.SetResultCallback(new ResultCB(fun r -> 
                if r.IsSuccess then
                    result:= None
                else
                    result := Some r.StatusMessage
                ev.Set() |> ignore
                ))
            let! isDone = Async.AwaitWaitHandle(ev, 1 * 60 * 1000)
            ev.Dispose()
            if not isDone then
                logE (sprintf "Timeout sending file %s" filePath)
            else
                match !result with
                | None -> 
                    logI (sprintf "file dataitem sent %s" filePath)
                | Some err -> logE (sprintf "Error sending file %s: %s" filePath err)
        with ex ->
            logE (sprintf "Error sending file %s: %s" filePath ex.Message)
    }

let sendFiles (ctx:Context) dir =
    let files = 
        Directory.GetFiles(dir) 
        |> Seq.filter (Storage.hasExt Storage.csv_ext)
        |> Seq.toList
    if files.Count() > 0 then
        let bldr = new GoogleApiClientBuilder(ctx)
        let gapi = bldr.AddApi(WearableClass.Api).Build()
        if not gapi.IsConnected then
            let r = gapi.BlockingConnect(30L,Java.Util.Concurrent.TimeUnit.Seconds)
            if r.IsSuccess then
                files 
                |> List.map (sendFile ctx gapi)
                |> Async.Parallel
                |> Async.Ignore
                |> Async.Start
            else
                logE (sprintf "unable to send files error code: %d" r.ErrorCode)
 