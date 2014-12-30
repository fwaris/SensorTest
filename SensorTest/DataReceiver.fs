module DataReceiver

open Extensions
open System
open System.Collections.Generic
open System.Linq
open System.Text
open System.IO

open Android.App
open Android.Content
open Android.OS
open Android.Runtime
open Android.Views
open Android.Widget
open Android.Gms.Wearable
open Android.Hardware
open Android.Gms.Common.Apis
open Android.Gms.Common.Data

let deleteDataItem gapi uri =
    async {
        let pr2 = WearableClass.DataApi.DeleteDataItems(gapi,uri)
        let! _  = GmsExtensions.awaitPending pr2 1000
        ()
        }

let processDataItem gapi (dt:IDataItem) =
    async {
        for kv in dt.Assets do
            let pr = WearableClass.DataApi.GetFdForAsset(gapi,kv.Value)
            let! fdr = GmsExtensions.awaitPending<IDataApiGetFdForAssetResult> pr 1000
            match fdr with
            | GmsExtensions.ARError _ -> ()
            | GmsExtensions.ARSuccess fdr ->
                let fileName = kv.Key
                let filePath = Path.Combine(Storage.data_directory,fileName)
                if not (File.Exists filePath) then
                    AndroidExtensions.toastShort (sprintf "Done %s" fileName)
                    logI (sprintf "saving %s to %s" fileName filePath)
                    use outStr = File.Create filePath
                    fdr.InputStream.CopyTo(outStr)
                    outStr.Close()
                    AndroidExtensions.toastShort (sprintf "%s transferred" fileName)
                else
                   logI (sprintf "file already exists %s" filePath) 
    }

let processDataEvents events =
    Storage.ensureDir Storage.data_directory
    let dataItems =
        events 
        |> Seq.cast<Java.Lang.Object>
        |> Seq.map    (fun i  -> i.JavaCast<IDataEvent>())
        |> Seq.filter (fun ev -> ev.DataItem.Uri.Path.StartsWith(Constants.p_data_file))
        |> Seq.filter (fun ev -> ev.Type = DataEvent.TypeChanged)
        |> Seq.map    (fun ev -> ev.DataItem)
        |> Seq.toArray
    let uris = dataItems |> Array.map (fun dt -> dt.Uri)
    async {
         try
             let! gapi = GmsExtensions.connect()
             for dt in dataItems do
                do! processDataItem gapi dt
             for uri in uris do
                 do! deleteDataItem gapi uri
         with ex -> 
             logE ex.Message} 
    |> Async.Start
 
let processPendingItems gapi =
    Storage.ensureDir Storage.data_directory
    async {
        let pr = WearableClass.DataApi.GetDataItems gapi
        let! dib = GmsExtensions.awaitPendingT<DataItemBuffer> pr 5000
        let dataItems = 
            [for i in 0..dib.Count-1 -> dib.Get(i).JavaCast<IDataItem>()]
            |> List.filter (fun dt -> dt.Uri.Path.StartsWith(Constants.p_data_file))
        let uris = dataItems |> List.map (fun x -> x.Uri)
        for dt in dataItems do
             do! processDataItem gapi dt
        dib.Release()
        for uri in uris do 
            do! deleteDataItem gapi uri
        }
 