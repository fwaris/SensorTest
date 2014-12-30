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

let tryDeleteFile path =
    try
        if File.Exists path then
            File.Delete path
            logI (sprintf "deleted %s" path)
    with ex ->
        let msg = sprintf "error deleting %s" path
        logE msg

let deleteFiles files dir =
    files
    |> List.map (fun f -> Path.Combine(dir,f))
    |> List.iter tryDeleteFile

let sendFile apiClient filePath =
    logI ("sending file " + filePath)
    async {
        try
            let fileName = Path.GetFileName(filePath)
            let dataRequest = PutDataRequest.Create(Constants.p_data_file + "/" + fileName)
            let contentUri = Android.Net.Uri.Parse(Storage.contentUri fileName)
            let permissionFlags = ActivityFlags.GrantReadUriPermission ||| ActivityFlags.GrantPersistableUriPermission
            let plyServPkg = GooglePlayServicesUtil.GooglePlayServicesPackage
            let ctx = Application.Context
            ctx.GrantUriPermission(plyServPkg,contentUri,permissionFlags)
            let asset = Asset.CreateFromUri(contentUri)
            dataRequest.PutAsset(fileName,asset) |> ignore
            let pr = WearableClass.DataApi.PutDataItem(apiClient,dataRequest)
            let dtr = GmsExtensions.awaitPendingT<IDataApiDataItemResult> pr 60000
            logI (sprintf "file dataitem sent %s" filePath)
        with ex ->
            logE (sprintf "Error sending file %s: %s" filePath ex.Message)
    }

let sendFiles files =
    async {
        try
            let! gapi = GmsExtensions.connect()
            let rec loop files =
                async {
                    match files with
                    | [] -> ()
                    | f::rest  ->
                        do! sendFile gapi f
                        return! loop rest }
            do! loop files
        with ex ->
            logE (sprintf "error sending files %s" ex.Message)
        }

let checkSendFiles dir =
    let files = 
        Directory.GetFiles(dir) 
        |> Seq.filter (Storage.hasExt Storage.csv_ext)
        |> Seq.toList
    if files.Count() > 0 then
        sendFiles files |> Async.Start
