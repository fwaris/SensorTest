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

let processDataItem (ctx:Context) (dt:IDataItem) =
    try
        let bldr = new GoogleApiClientBuilder(ctx)
        let apiClient = bldr.AddApi(WearableClass.Api).Build()
        let r = apiClient.BlockingConnect()
        if not r.IsSuccess then failwith "no connection"
        dt.Assets
        |> Seq.iter (fun kv ->
            let pi = WearableClass.DataApi.GetFdForAsset(apiClient,kv.Value)
            let fdr = pi.Await().JavaCast<IDataApiGetFdForAssetResult>()
            let fileName = kv.Key
            let filePath = Path.Combine(Storage.data_directory,fileName)
            use outStr = File.Create filePath
            fdr.InputStream.CopyTo(outStr)
            outStr.Close()
            )
    with ex -> 
       logE ex.Message


        

