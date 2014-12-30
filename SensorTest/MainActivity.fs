namespace SensorTest

open System
open Android.App
open Android.Content
open Android.OS
open Android.Runtime
open Android.Views
open Android.Widget
open Android.Hardware
open Extensions

[<Activity(Label = "SensorTest", MainLauncher = true)>]
type MainActivity() = 
    inherit Activity()
    let uiCtx = System.Threading.SynchronizationContext.Current

    let sensors = 
        [|
            SensorType.Accelerometer; SensorType.Gyroscope; 
            SensorType.LinearAcceleration
            SensorType.RotationVector; SensorType.Gravity
        |]

    let withProtectionDo comp uiUpdate =
        async {
            try
                do! comp
            with ex ->
                logE ex.Message
            do! Async.SwitchToContext uiCtx
            do uiUpdate()
        } |> Async.Start


    override this.OnCreate(bundle) = 
        base.OnCreate(bundle)
        this.SetContentView(Resource_Layout.Main)
        // 
        let btncopy     = this.FindViewById<Button>(Resource_Id.btnCopy)
        let btnTxFiles  = this.FindViewById<Button>(Resource_Id.btnTransferFiles)
        let btnTrigger  = this.FindViewById<Button>(Resource_Id.btnTrigger)
        //
        btnTrigger.Click.Add(fun _ ->
            btnTrigger.Enabled <- false
            withProtectionDo
                (GmsExtensions.sendWearMessage Constants.p_start [||])
                (fun () -> btnTrigger.Enabled <- true)
            )
        //
        btncopy.Click.Add(fun _ ->
            btncopy.Enabled <- false
            Storage.copyFile this uiCtx (fun _ _ -> btncopy.Enabled <- true)
            )
        //
        btnTxFiles.Click.Add(fun _ ->
            btnTxFiles.Enabled <- false
            let comp =
                async {
                    let! gapi = GmsExtensions.connect()
                    do! DataReceiver.processPendingItems gapi
                    do! GmsExtensions.sendWearMessage Constants.p_send_data [||]}
            withProtectionDo comp (fun () -> btnTxFiles.Enabled <- true)
            )

    override this.OnDestroy() =
        base.OnDestroy()
 