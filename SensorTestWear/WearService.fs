namespace SensorTestWear

open Extensions
open System
open System.Collections.Generic
open System.Linq
open System.Text
open Android.App
open Android.Content
open Android.OS
open Android.Runtime
open Android.Views
open Android.Widget
open Android.Gms.Wearable
open Android.Hardware
open Android.Gms.Common.Apis

type ResultCB() =
    inherit Java.Lang.Object() 
    interface IResultCallback with
        member x.OnResult(a) =
            let r = a
            () 

[<Service>] 
[<IntentFilter([|WearableListenerService.BindListenerIntentAction|])>]
type HeartRateService() as this = 
    inherit WearableListenerService()
    let mutable snsrs: Sensor array  = Unchecked.defaultof<_>
    let mutable gapi : IGoogleApiClient = Unchecked.defaultof<_>
    let mutable nodeId = ""
    let mutable wakeLock:PowerManager.WakeLock = null
    let heart_rate_sensor_type = enum<SensorType>(65562)

    let unregister() =
        let smgr = this.GetSystemService(Service.SensorService) :?> SensorManager
        if smgr <> Unchecked.defaultof<_> then
            smgr.UnregisterListener(this)
            snsrs <- Unchecked.defaultof<_>

    let register() = 
        if snsrs = Unchecked.defaultof<_> then
            let smgr = this.GetSystemService(Service.SensorService) :?> SensorManager
            let l = smgr.GetSensorList(SensorType.All) |> Seq.map (fun x-> x.ToString()) |> Seq.toArray
            let xs = 
                    [|
                        SensorType.Accelerometer; SensorType.Gyroscope; 
                        SensorType.LinearAcceleration; SensorType.Orientation
                    |]
            snsrs <- xs |> Array.map (smgr.GetDefaultSensor)
            for snsr in snsrs do
                smgr.RegisterListener(this, snsr, SensorDelay.Normal) |> ignore
            ()

    let acquireWakeLock() =
        let pm = this.GetSystemService(Service.PowerService) :?> PowerManager
        wakeLock <- pm.NewWakeLock(WakeLockFlags.Partial, "SensorTest")
        wakeLock.Acquire()

    let releaseWakeLock() =
        if wakeLock <> null then
            wakeLock.Release()
            wakeLock.Dispose()
        wakeLock <- null

 (* 
    let sendWearMessage path data =
        async {
            try
            let r = gapi.BlockingConnect(30L,Java.Util.Concurrent.TimeUnit.Seconds)
            if r.IsSuccess then
                let pr = WearableClass.NodeApi.GetConnectedNodes(gapi)
                let nr =  pr.Await().JavaCast<INodeApiGetConnectedNodesResult>()
                for n in nr.Nodes do
                    let pr2 = WearableClass.MessageApi.SendMessage(gapi,n.Id,path,data)
                    let s = pr2.Await(1L,Java.Util.Concurrent.TimeUnit.Seconds)
                    let r = s.JavaCast<Android.Gms.Wearable.IMessageApiSendMessageResult>()
                    if not r.Status.IsSuccess then
                        logI (sprintf "HR Error %d" r.Status.StatusCode)
                        logI r.Status.StatusMessage
                    else
                        logI (sprintf "HR success %s" n.Id)
            with ex -> 
                    logE ex.Message
        }
  *)  

    interface ISensorEventListener with
        member x.OnAccuracyChanged(_,_) = ()
        member x.OnSensorChanged(ev) = 
            logI (sprintf "%A" ev.Sensor.Type)
            for v in ev.Values do logI (sprintf "%f" v)

    override x.OnCreate() = 
        base.OnCreate()
        let bldr = new GoogleApiClientBuilder(x)
        gapi <- bldr.AddApi(WearableClass.Api).Build()

    override x.OnStart(intnt,id) =
        base.OnStart(intnt,id)
        try acquireWakeLock() with ex -> logE ex.Message 
        register()

    override x.OnMessageReceived(msg) =
            let d = msg.GetData()
            let p = msg.Path
            let s = msg.SourceNodeId
            match p with
            | "/start" -> 
                logI "/start"
                register()
                nodeId <- s
                new Intent(this,typeof<HeartRateService>) |> x.StartService |> ignore
            | "/stop" ->
                logI "/stop"
                unregister()
                new Intent(this,typeof<HeartRateService>) |> x.StopService |> ignore
            | p -> 
                logE (sprintf "invalid path %s" p)
           
    override x.OnDestroy() =
        try 
            unregister() 
            releaseWakeLock()
        with _ -> ()
        base.OnDestroy()


            

