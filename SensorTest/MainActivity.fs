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
    let mutable subscription = Unchecked.defaultof<_>

    let isrunning() = AndroidExtensions.isServiceRunning "SensorTest.HostSensorService"

    let sensors = 
        [|
            SensorType.Accelerometer; SensorType.Gyroscope; 
            SensorType.LinearAcceleration
            SensorType.RotationVector; SensorType.Gravity
        |]


    let updateService (ctx:Context) (lv:ListView) (sw:Switch) =
        async {
            try 
                let isrunning = isrunning()
                do! Async.SwitchToContext uiCtx
                sw.Checked <- isrunning
                sw.Click.Add (fun ev -> 
                    if sw.Checked then
                        let itemIds = lv.CheckedItemPositions
                        let sz = itemIds.Size()
                        let itemIds = [|for i in 0..sz - 1 do if itemIds.Get(i) then yield i |]
                        let selected = itemIds |> Array.map (fun x->int sensors.[int x])
                        let bytes = Serdes.intArrayToBytes selected
                        let i = new Intent(ctx,typeof<HostSensorService>)
                        i.PutExtra(Constants.sensors,bytes)
                        |> ctx.StartService |> ignore
                    else
                        new Intent(ctx,typeof<HostSensorService>) |> ctx.StopService |> ignore
                    )
            with ex ->
                logE ex.Message
            } |> Async.Start

    let filePrefixes =
        [|
            ""
            "driving"
            "twist"
            "flick_up"
            "sidewave"
            "flick_down"
        |]

    override this.OnCreate(bundle) = 
        base.OnCreate(bundle)
        this.SetContentView(Resource_Layout.Main)
        // 
        let btncopy     = this.FindViewById<Button>(Resource_Id.btnCopy)
        let spnFiles    = this.FindViewById<Spinner>(Resource_Id.spnFiles)
        let switch      = this.FindViewById<Switch>(Resource_Id.swService)
        let snsrList    = this.FindViewById<ListView>(Resource_Id.sensorList)
        let btnTxFiles  = this.FindViewById<Button>(Resource_Id.btnTransferFiles)
        //
        let adapter = new ArrayAdapter<string>(this,Android.Resource.Layout.SimpleListItem1,filePrefixes)
        spnFiles.Adapter <- adapter
        spnFiles.ItemSelected.Add (fun ev ->
            try
                let f = filePrefixes.[ev.Position]
                GlobalState.filenameprefix <- f
            with ex ->
               logE ex.Message
            )
        //
        let slist = sensors |> Array.map (sprintf "%A")
        let sadapter = new ArrayAdapter<string>(this,Android.Resource.Layout.SimpleListItemMultipleChoice,slist)
        snsrList.ChoiceMode <- ChoiceMode.Multiple
        snsrList.Adapter <- sadapter
        for p in 0..sadapter.Count-1 do snsrList.SetItemChecked(p,true)
        //
        btncopy.Click.Add(fun _ ->
            if isrunning() then
               AndroidExtensions.notifyAsync this uiCtx "" "Please stop service" (fun _ _->())
            else
                btncopy.Enabled <- false
                Storage.copyFile this uiCtx (fun _ _ -> btncopy.Enabled <- true)

            )
        btnTxFiles.Click.Add(fun _ -> AndroidExtensions.sendWearMessage this Constants.p_send_data [||] |> Async.Start)
           
        updateService this snsrList switch
        subscription <- GlobalState.isRunning.Subscribe(fun running -> 
            logI (sprintf "global state changed: %A" running)
            switch.Checked <- running)


    override this.OnDestroy() =
        base.OnDestroy()
        if subscription <> Unchecked.defaultof<_> then subscription.Dispose()