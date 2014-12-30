namespace SensorTest

open Extensions
open System
open System.Collections.Generic
open System.Linq
open System.Text
open System.IO
open AndroidExtensions

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

[<Service>] 
[<IntentFilter([|WearableListenerService.BindListenerIntentAction|])>]
type HostSensorService() = 
    inherit WearableListenerService()

    override x.OnMessageReceived(msg) =
            let d = msg.GetData()
            let p = msg.Path
            let s = msg.SourceNodeId
            match p with
            | p -> 
                logE (sprintf "invalid path %s" p)
           
    override x.OnDataChanged(dataEvents) =
        logI "onDataChanged"
        try
            let events = FreezableUtils.FreezeIterable (dataEvents)
            dataEvents.Close()
            DataReceiver.processDataEvents events
        with ex ->
           logE ex.Message
 