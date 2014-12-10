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

type WearableListItemLayout(ctx:Context) =
    inherit LinearLayout(ctx) 
    let mutable scale = 1.0f
    let fadedColor = Resource_Color.grey
    let chosenColor = Resource_Color.blue
    let fadeAlpha = 0.4f;
    let mutable circle : ImageView = null
    let mutable name   : TextView  = null
 
    interface WearableListView.IItem with 
        member x.ProximityMinValue = 1.0f
        member x.ProximityMaxValue = 1.6f
        member x.SetScalingAnimatorValue(v) = 
            scale <- v
            circle.ScaleX <- v
            circle.ScaleY <- v
        member x.CurrentProximityValue = scale
        member x.OnScaleUpStart() = 
            name.Alpha <- 1.f
            (circle.Drawable :?> GradientDrawable).SetColor(chosenColor)
        member x.OnScaleDownStart() = 
            name.Alpha <- fadeAlpha
            (circle.Drawable :?> GradientDrawable).SetColor(fadedColor)

    override x.OnFinishInflate() =
        base.OnFinishInflate()
        circle <- x.FindViewById<ImageView>(Resource_Id.circle)
        name <- x.FindViewById<TextView>(Resource_Id.name)

[<Activity(Label = "Sensor Test", MainLauncher = true)>]
type MainActivity() = 
    inherit Activity()
    let mutable count : int = 1
    let uiCtx = System.Threading.SynchronizationContext.Current
    let mutable subscription = Unchecked.defaultof<_>
    let defaultSensors = 
            [|
                SensorType.Accelerometer; SensorType.Gyroscope; 
                SensorType.LinearAcceleration
                SensorType.RotationVector; SensorType.Gravity
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
                        let data = defaultSensors |> Array.map int
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
        this.SetContentView(Resource_Layout.Main)
        let btn = this.FindViewById<Button>(Resource_Id.btnSensors)
        let sw = this.FindViewById<Switch>(Resource_Id.swService)
        updateService this sw 
        btn.Click.Add(fun _ -> new Intent(this,typeof<PageViewActivity>) |> this.StartActivity)
        subscription <- GlobalState.isRunning.Subscribe(fun running -> 
            logI (sprintf "global state changed: %A" running)
            sw.Checked <- running)

    override this.OnStop() = 
       if subscription <> Unchecked.defaultof<_> then subscription.Dispose()
       base.OnStop()


