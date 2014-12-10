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

type ResultCB(fDone) =
    inherit Java.Lang.Object() 
    interface IResultCallback with
        member x.OnResult(result) =
            let r = result.JavaCast<IDataApiDataItemResult>();
            fDone r.Status

let sendFile apiClient filePath =
    logI ("sending file " + filePath)
    async {
        try
            let dataItem = PutDataRequest.Create(Constants.p_data_file)
            let fileName = Path.GetFileName(filePath)
            let asset = Asset.CreateFromUri(Android.Net.Uri.Parse("file://" + filePath))
            dataItem.PutAsset(fileName,asset) |> ignore
            let ev = new ManualResetEvent(false)
            let result = ref (Some "waiting")
            let pendingRequest = WearableClass.DataApi.PutDataItem(apiClient,dataItem)
            pendingRequest.SetResultCallback(new ResultCB(fun r -> 
                if r.IsSuccess then
                    result:= None
                else
                    result := Some r.StatusMessage
                ev.Set() |> ignore
                ))
            let! isDone = Async.AwaitWaitHandle(ev, 15 * 60 * 1000)
            ev.Dispose()
            if not isDone then
                logE (sprintf "Timeout sending file %s" filePath)
            else
                match !result with
                | None -> 
                    logI (sprintf "file sent %s" filePath)
                    File.Delete filePath
                | Some err -> logE (sprintf "Error sending file %s: %s" filePath err)
        with ex ->
            logE (sprintf "Error sending file %s: %s" filePath ex.Message)
    }

let sendFiles (ctx:Context) dir =
    let files = 
        Directory.GetFiles(dir) 
        |> Seq.filter (fun f -> Path.GetExtension(f) = Storage.csv_ext)
        |> Seq.toList
    if files.Count() > 0 then
        let bldr = new GoogleApiClientBuilder(ctx)
        let apiClient = bldr.AddApi(WearableClass.Api).Build()
        files 
        |> List.map (sendFile apiClient)
        |> Async.Parallel
        |> Async.Ignore
        |> Async.Start
 