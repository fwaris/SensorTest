namespace SensorTest

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

type ResultCB() =
    inherit Java.Lang.Object() 
    interface IResultCallback with
        member x.OnResult(a) =
            let r = a
            () 

type WMsg = Fire of (string*byte array) | Send of (string*byte array*AsyncReplyChannel<unit>)

[<Service>] 
[<IntentFilter([|WearableListenerService.BindListenerIntentAction|])>]
type HostSensorService() as this = 
    inherit WearableListenerService()
    let mutable gapi : IGoogleApiClient = Unchecked.defaultof<_>
    let mutable wakeLock:PowerManager.WakeLock = null
    let mutable cts = Unchecked.defaultof<_>


    let acquireWakeLock() =
        let pm = this.GetSystemService(Service.PowerService) :?> PowerManager
        wakeLock <- pm.NewWakeLock(WakeLockFlags.Partial, "SensorTest")
        wakeLock.Acquire()

    let releaseWakeLock() =
        if wakeLock <> null then
            wakeLock.Release()
            wakeLock.Dispose()
        wakeLock <- null

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
                        logI (sprintf "msg send error %d" r.Status.StatusCode)
                        logI r.Status.StatusMessage
            with ex -> 
                    logE ex.Message
        }

    let sendLoop (inbox:MailboxProcessor<_>) =
        async {
            while true do
                try 
                    let! msg  = inbox.Receive()
                    match msg with
                    | Fire (path,data) ->
                        do! sendWearMessage path data
                    | Send (path,data,rc) ->
                        do! sendWearMessage path data
                        rc.Reply()
                with ex ->
                    logE ex.Message
         }

    let mutable sendAgent : MailboxProcessor<_> = Unchecked.defaultof<_>

    override x.OnCreate() = 
        base.OnCreate()
        let bldr = new GoogleApiClientBuilder(x)
        gapi <- bldr.AddApi(WearableClass.Api).Build()

    override x.OnStart(intnt,id) =
        base.OnStart(intnt,id)
        try
            let data = intnt.GetByteArrayExtra(Constants.sensors)
            acquireWakeLock()
            //Storage.agent.Post( Storage.FOpen GlobalState.filenameprefix )
            cts <- new System.Threading.CancellationTokenSource()
            sendAgent <- MailboxProcessor.Start(sendLoop,cts.Token)
            sendAgent.Post (Fire (Constants.p_start,data))
            GlobalState.runningEvent.Trigger(true)
            logI "service started"
        with ex ->
            logE ex.Message

    override x.OnMessageReceived(msg) =
            let d = msg.GetData()
            let p = msg.Path
            let s = msg.SourceNodeId
            match p with
            | Constants.p_sensor -> ()
//                let ticks = BitConverter.ToInt64(d,0)
//                let st = BitConverter.ToInt32(d,8)
//                let stt = enum<SensorType>(st)
//                let count = BitConverter.ToInt32(d,12)
//                let values = [|for i in 0..count-1 -> BitConverter.ToSingle(d, 4 * i + 16)|]
//                (ticks,Enum.GetName(typeof<SensorType>,stt),count,values) |> Storage.Data |> Storage.agent.Post
            | p -> 
                logE (sprintf "invalid path %s" p)
           
    override x.OnDestroy() =
        try 
            sendWearMessage Constants.p_send_data [||] |> Async.Start
            //Storage.agent.PostAndReply Storage.FClose
            if sendAgent <> Unchecked.defaultof<_> then
                sendAgent.PostAndReply  (fun rc -> Send (Constants.p_stop,[||],rc))
            releaseWakeLock()
            cts.CancelAfter(1000)
            GlobalState.runningEvent.Trigger(false)
            logI "service closed"
        with ex ->
            logE (sprintf "error stopping service %s" ex.Message)
        base.OnDestroy()

    override x.OnDataChanged(dataEvents) =
        base.OnDataChanged(dataEvents)
        logI "onDataChanged"
        let events = FreezableUtils.FreezeIterable (dataEvents)
        events 
        |> Seq.cast<Java.Lang.Object>
        |> Seq.map    (fun i  -> i.JavaCast<IDataEvent>())
        |> Seq.filter (fun ev -> ev.Type = DataEvent.TypeChanged)
        |> Seq.map    (fun ev -> ev.DataItem)
        |> Seq.filter (fun dt -> printfn "%s" dt.Uri.Path; dt.Uri.Path = Constants.p_data_file)
        |> Seq.iter (DataReceiver.processDataItem x)
 