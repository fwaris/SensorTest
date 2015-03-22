namespace SensorTestWear

open System
open Extensions
open Android.App
open Android.Content
open Android.OS
open Android.Runtime
open Android.Views
open Android.Widget
open Android.Gms.Wearable
open Android.Hardware
open Android.Support.Wearable.Views
open Android.Graphics.Drawables
open Android.Gms.Wearable
open Android.Hardware


[<Activity(Label = "Sensor Test", MainLauncher = true)>]
type MainActivity() = 
    inherit Activity()
    let mutable count : int = 1
    let uiCtx = System.Threading.SynchronizationContext.Current
    let mutable subscription = Unchecked.defaultof<_>

    let filePrefixes =
        [|
            "driving"
            "twist"
            "tap"
            "swipe"
            "left"
            "right"
            "right-tap"
            "right-swipe"
            "left-tap"
            "left-swipe"
        |]

    let updateService (ctx:Context) (sw:Switch) =
        async {
            try 
                let isrunning = AndroidExtensions.isServiceRunning "SensorTestWear.WearSensorService"
                do! Async.SwitchToContext uiCtx
                sw.Checked <- isrunning
                sw.Click.Add (fun ev -> 
                    if sw.Checked then
                        let i = new Intent(ctx,typeof<WearSensorService>) 
                        let data = ServiceContants.defaultSensors |> Array.map int
                        i.PutExtra(Constants.sensors, Serdes.intArrayToBytes data)
                        |> ctx.StartService |> ignore
                    else
                        new Intent(ctx,typeof<WearSensorService>) |> ctx.StopService |> ignore
                    )
            with ex ->
                logE ex.Message
            } |> Async.Start

    override this.OnCreate(bundle) = 
        base.OnCreate(bundle)
        Storage.ensureDir(Storage.data_directory)
        this.SetContentView(Resource_Layout.Main)

        let btnSensors = this.FindViewById<Button>(Resource_Id.btnSensors)
        let tglService = this.FindViewById<Switch>(Resource_Id.swService)
        let btnFiles   = this.FindViewById<Button>(Resource_Id.btnFiles)
        let btnDelFls  = this.FindViewById<Button>(Resource_Id.btnDelFiles)
        let spnFiles   = this.FindViewById<Spinner>(Resource_Id.spnFiles)
 
        let adapter = new ArrayAdapter<string>(this,Android.Resource.Layout.SimpleListItem1,filePrefixes)
        spnFiles.Adapter <- adapter
        spnFiles.ItemSelected.Add (fun ev ->
            try
                let f = filePrefixes.[ev.Position]
                GlobalState.filenameprefix <- f
            with ex ->
               logE ex.Message
            )
        spnFiles.SetSelection(0)

        updateService this tglService
        subscription <- GlobalState.isRunning.Subscribe(fun running -> 
            logI (sprintf "global state changed: %A" running)
            tglService.Checked <- running)

        btnSensors.Click.Add (fun _ -> new Intent(this,typeof<PageViewActivity>) |> this.StartActivity )

        btnFiles.Click.Add (fun _ -> new Intent(this,typeof<ListFilesActivity>)  |> this.StartActivity)

        btnDelFls.Click.Add (fun _ -> 
            AndroidExtensions.promptAsync 
                this
                uiCtx
                "Alert"
                "Delete all files?"
                (fun  _ _ -> Storage.deleteFiles())
                (fun _  _ -> ())
            )

    override this.OnDestroy() = 
       base.OnDestroy()
       if subscription <> Unchecked.defaultof<_> then subscription.Dispose()
       base.OnStop()


