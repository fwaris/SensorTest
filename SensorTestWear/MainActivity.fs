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

(* awaiting a xamarin fix for lit view

type WlvAdapter(ctx:Context, items : string array) =
    inherit WearableListView.Adapter()
    let layoutInflator = LayoutInflater.From(ctx)

    override x.ItemCount = items.Length

    override x.OnCreateViewHolder(parent,viewType) =
        new WearableListView.ViewHolder(
            layoutInflator.Inflate(Resource_Layout.ListItem,parent))
        :> _

    override x.OnBindViewHolder(holder,pos) =
        let view = holder.ItemView.FindViewById<TextView>(Resource_Id.name)
        view.Text <- (items.[pos])

*)

[<Activity(Label = "SensorTestWear", MainLauncher = true)>]
type MainActivity() = 
    inherit Activity()
    let mutable count : int = 1

    override this.OnCreate(bundle) = 

        base.OnCreate(bundle)
        // Set our view from the "main" layout resource
        this.SetContentView(Resource_Layout.Main)
        // Get our button from the layout resource, and attach an event to it
        let btn = this.FindViewById<Button>(Resource_Id.btnSensors)
        let sw = this.FindViewById<Switch>(Resource_Id.swService)
        btn.Click.Add(fun _ ->
            new Intent(this,typeof<PageViewActivity>) |> this.StartActivity
            )





(*
    override this.OnCreate(bundle) = 

        base.OnCreate(bundle)
        // Set our view from the "main" layout resource
        this.SetContentView(Resource_Layout.Main)
        // Get our button from the layout resource, and attach an event to it
        let button = this.FindViewById<Button>(Resource_Id.btnSensors)
        let sw = this.FindViewById<Switch>(Resource_Id.swService)
        let ll = button.Parent :?> LinearLayout
        let wlv = new WearableListView(this)
        wlv.LayoutParameters <-
            new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MatchParent,
                LinearLayout.LayoutParams.MatchParent)
        ll.AddView(wlv)
        button.Click.Add(fun args -> 
            let smgr = this.GetSystemService(Service.SensorService) :?> SensorManager
            let l = smgr.GetSensorList(SensorType.All) |> Seq.map (fun x-> x.ToString()) |> Seq.toArray

            button.Text <- sprintf "%d clicks!" count
            count <- count + 1)
*)

    override this.OnStop() = 
       base.OnStop()

    //this is only needed for
    //the linker to not stip out the "OnCompleted" method
    interface IObserver<string> with
        member x.OnCompleted() = ()
        member x.OnError(y) = ()
        member x.OnNext(y) = ()

       
