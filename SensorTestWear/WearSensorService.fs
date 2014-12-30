namespace SensorTestWear

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

module ServiceContants =
    let defaultSensors = 
            [|
                SensorType.Accelerometer; SensorType.Gyroscope; 
                SensorType.LinearAcceleration
                SensorType.RotationVector; SensorType.Gravity
            |]

[<Service>] 
[<IntentFilter([|WearableListenerService.BindListenerIntentAction|])>]
type WearSensorService() as this = 
    inherit WearableListenerService()
    let mutable snsrs: Sensor array  = Unchecked.defaultof<_>
    let mutable nodeId = ""
    let mutable wakeLock:PowerManager.WakeLock = null
    let mutable cts = Unchecked.defaultof<_>
    let mutable storageAgent = Unchecked.defaultof<_>
    let mutable sendFilesAgent = Unchecked.defaultof<_>
    let mutable messageSendAgent : MailboxProcessor<byte array> = Unchecked.defaultof<_>

    let unregister() =
        let smgr = this.GetSystemService(Service.SensorService) :?> SensorManager
        if smgr <> Unchecked.defaultof<_> then
            smgr.UnregisterListener(this)
            snsrs <- Unchecked.defaultof<_>

    let register sensors = 
        if snsrs = Unchecked.defaultof<_> then
            let smgr = this.GetSystemService(Service.SensorService) :?> SensorManager
            let l = smgr.GetSensorList(SensorType.All) |> Seq.map (fun x-> x.ToString()) |> Seq.toArray

            snsrs <- sensors |> Array.map (smgr.GetDefaultSensor)
            for snsr in snsrs do
                smgr.RegisterListener(this, snsr, SensorDelay.Game) |> ignore
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

    let sendLoop (inbox:MailboxProcessor<byte array>) =
        async {
            while true do
                try 
                    let! msg = inbox.Receive()
                    do! GmsExtensions.sendWearMessage Constants.sensors msg
                with ex ->
                    logE ex.Message
         }

    let prepMessage (ev:SensorEvent) = 
        //let dt = BitConverter.GetBytes(DateTime.UtcNow.Ticks)
        let dt = BitConverter.GetBytes(ev.Timestamp)
        let st = int ev.Sensor.Type
        let count = ev.Values.Count
        let size = 16 + count * 4 //16 = date time: 8 + sensor type:4 + count:4)
        let bytes = Array.zeroCreate size
        Array.Copy(dt,0,bytes,0,8)
        let bSt = BitConverter.GetBytes(st)
        Array.Copy(bSt,0,bytes,8,4)
        let szBts = BitConverter.GetBytes(count)
        Array.Copy(szBts,0,bytes,12,4)
        let mutable c = 16
        for v in ev.Values do
            let bts = BitConverter.GetBytes(v)
            Array.Copy(bts,0,bytes,c,4)
            c <- c + 4
        bytes            

    interface ISensorEventListener with
        member x.OnAccuracyChanged(_,_) = ()
        member x.OnSensorChanged(ev) = 
            let msg = prepMessage ev
            if storageAgent.CurrentQueueLength < 100 then
                let msg = (ev.Timestamp,int ev.Sensor.Type, ev.Values.Count, ev.Values)
                storageAgent.Post (Storage.Data msg)
            else
                logE "queue depth exceeded"

    override x.OnCreate() = 
        base.OnCreate()

    override x.OnStart(intnt,id) =
        base.OnStart(intnt,id)
        try
            let data = intnt.GetByteArrayExtra(Constants.sensors)
            let sensors = data |> Serdes.bytesToIntArray |> Array.map enum<SensorType>
            let sensors = if Array.isEmpty sensors then ServiceContants.defaultSensors else sensors
            acquireWakeLock()
            cts <- new System.Threading.CancellationTokenSource()
            messageSendAgent <- MailboxProcessor.Start(sendLoop,cts.Token)
            storageAgent <- MailboxProcessor.Start(Storage.writeLoop,cts.Token)
            storageAgent.Post (Storage.FOpen GlobalState.filenameprefix)
            Storage.rollover (5000*60) storageAgent |> Async.Start
            register sensors
            GlobalState.runningEvent.Trigger(true)
            logI "service started"
        with ex ->
            logE ex.Message

    override x.OnMessageReceived(msg) =
        try
            let d = msg.GetData()
            let p = msg.Path
            let s = msg.SourceNodeId
            logI p
            match p with
            | Constants.p_start -> 
                nodeId <- s
                let i = new Intent(this,typeof<WearSensorService>)
                i.PutExtra(Constants.sensors,d)
                |> x.StartService |> ignore
            | Constants.p_stop ->
                this.StopSelf()
            | Constants.p_send_data ->
                FileSender.checkSendFiles Storage.data_directory
            | p -> 
                logE (sprintf "invalid path %s" p)
        with ex ->
            logE ex.Message
                           
    override x.OnDestroy() =
        try 
            unregister() 
            if storageAgent <> Unchecked.defaultof<_> then 
                storageAgent.PostAndReply Storage.FClose
            releaseWakeLock()
            if cts <> Unchecked.defaultof<_> then cts.Cancel()
            GlobalState.runningEvent.Trigger(false)
            messageSendAgent <- Unchecked.defaultof<_>
            storageAgent <- Unchecked.defaultof<_>
            logI "service closed"
        with ex ->
            logE (sprintf "error stopping service %s" ex.Message)
        base.OnDestroy()

    override x.OnDataChanged(eventBuffer) =
        logI "onDataChanged"
        try
            let dataEvents = [for i in 0..eventBuffer.Count-1 -> eventBuffer.Get(i).JavaCast<IDataEvent>()]
            let files =
                dataEvents
                |> Seq.filter (fun ev -> ev.Type = DataEvent.TypeDeleted)
                |> Seq.map    (fun ev -> ev.DataItem.Uri.Path)
                |> Seq.map    (fun p  -> p.Remove(0,Constants.p_data_file.Length + 1))
                |> Seq.toList
            eventBuffer.Close()
            FileSender.deleteFiles files Storage.data_directory
        with ex ->
           eventBuffer.Close()
           logE ex.Message

            

